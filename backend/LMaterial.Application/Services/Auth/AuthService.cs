using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Application.DTOs.Auth;
using LMaterial.Domain.Interfaces;
using LMaterial.Domain.Repositories;
using Microsoft.Extensions.Configuration;

namespace LMaterial.Application.Services.Auth;

public class AuthService : IAuthService
{
    private const int AccessTokenSegundosPadrao = 900;
    private static readonly TimeSpan RefreshTokenValidade = TimeSpan.FromDays(7);

    private readonly ITokenService _tokenService;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public AuthService(
        ITokenService tokenService,
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _usuarioRepository = usuarioRepository ?? throw new ArgumentNullException(nameof(usuarioRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return null;

        var usuario = await _usuarioRepository.GetByUsernameAsync(request.Username.Trim(), ct);
        if (usuario == null || !usuario.Ativo || !_passwordHasher.Verify(request.Password, usuario.SenhaHash))
            return null;

        var perfil = usuario.Perfil.ToString();
        var accessToken = _tokenService.GenerateAccessToken(usuario.Id, usuario.Username, perfil, out _);
        var refreshToken = _tokenService.GenerateRefreshToken();

        usuario.DefinirRefreshToken(HashRefreshToken(refreshToken), DateTime.UtcNow.Add(RefreshTokenValidade));
        await _unitOfWork.SaveChangesAsync(ct);

        return MontarResposta(accessToken, refreshToken, usuario.Username, usuario.NomeCompleto, perfil);
    }

    public async Task<LoginResponse?> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken) || string.IsNullOrWhiteSpace(request.RefreshToken))
            return null;

        ClaimsPrincipal? principal;
        try
        {
            principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        }
        catch
        {
            return null;
        }

        var subject = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(subject, out var usuarioId))
            return null;

        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId, ct);
        if (usuario == null || !usuario.Ativo || usuario.RefreshTokenHash == null || usuario.RefreshTokenExpiracao == null)
            return null;

        if (usuario.RefreshTokenHash != HashRefreshToken(request.RefreshToken) || usuario.RefreshTokenExpiracao <= DateTime.UtcNow)
        {
            // Token inválido ou expirado: revoga por segurança
            usuario.RevogarRefreshToken();
            await _unitOfWork.SaveChangesAsync(ct);
            return null;
        }

        // Rotação: a cada uso são gerados um novo access token e um novo refresh token
        var perfil = usuario.Perfil.ToString();
        var newAccessToken = _tokenService.GenerateAccessToken(usuario.Id, usuario.Username, perfil, out _);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        usuario.DefinirRefreshToken(HashRefreshToken(newRefreshToken), DateTime.UtcNow.Add(RefreshTokenValidade));
        await _unitOfWork.SaveChangesAsync(ct);

        return MontarResposta(newAccessToken, newRefreshToken, usuario.Username, usuario.NomeCompleto, perfil);
    }

    public async Task RevokeTokenAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId, ct);
        if (usuario == null)
            return;

        usuario.RevogarRefreshToken();
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private LoginResponse MontarResposta(string accessToken, string refreshToken, string username, string nomeCompleto, string perfil)
    {
        var expiracao = int.TryParse(_configuration["JwtSettings:ExpirationMinutes"], out var minutos)
            ? minutos * 60
            : AccessTokenSegundosPadrao;

        return new LoginResponse(accessToken, refreshToken, expiracao, username, nomeCompleto, perfil);
    }

    // Refresh tokens são guardados somente como hash SHA-256
    private static string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(bytes);
    }
}
