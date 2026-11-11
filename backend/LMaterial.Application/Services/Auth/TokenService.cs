using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LMaterial.Application.Services.Auth;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public string GenerateAccessToken(Guid usuarioId, string username, string perfil, out string jti)
    {
        jti = Guid.NewGuid().ToString();

        var issuer = _configuration["JwtSettings:Issuer"] ?? "LMaterial.API";
        var audience = _configuration["JwtSettings:Audience"] ?? "LMaterial.Clients";
        var expirationMinutesStr = _configuration["JwtSettings:ExpirationMinutes"] ?? "15";

        if (!int.TryParse(expirationMinutesStr, out var expirationMinutes))
        {
            expirationMinutes = 15;
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ObterSegredo()));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, perfil)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var issuer = _configuration["JwtSettings:Issuer"] ?? "LMaterial.API";
        var audience = _configuration["JwtSettings:Audience"] ?? "LMaterial.Clients";

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ObterSegredo())),
            ValidateLifetime = false // Permite extrair as claims de um access token expirado durante a rotação
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Token de acesso inválido ou com algoritmo incompatível.");
        }

        return principal;
    }

    // O segredo não tem valor padrão no código: vem de user-secrets, variável de ambiente ou cofre de segredos
    private string ObterSegredo()
    {
        var segredo = _configuration["JwtSettings:Secret"];
        if (string.IsNullOrWhiteSpace(segredo) || segredo.Length < 32)
            throw new InvalidOperationException("JwtSettings:Secret não configurado (mínimo de 32 caracteres). Use user-secrets ou variável de ambiente.");

        return segredo;
    }
}
