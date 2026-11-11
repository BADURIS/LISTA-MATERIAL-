using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LMaterial.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    // Identidade do usuário sempre vem do token autenticado, nunca de dados enviados pelo cliente
    public static Guid ObterUsuarioId(this ClaimsPrincipal user)
    {
        var valor = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!Guid.TryParse(valor, out var usuarioId))
            throw new UnauthorizedAccessException("Token sem identificação de usuário válida.");

        return usuarioId;
    }
}
