using System;
using System.Security.Claims;

namespace LMaterial.Application.Services.Auth;

public interface ITokenService
{
    string GenerateAccessToken(Guid usuarioId, string username, string perfil, out string jti);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
