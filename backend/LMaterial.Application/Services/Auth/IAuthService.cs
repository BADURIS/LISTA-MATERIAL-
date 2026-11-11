using System;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Application.DTOs.Auth;

namespace LMaterial.Application.Services.Auth;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<LoginResponse?> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task RevokeTokenAsync(Guid usuarioId, CancellationToken ct = default);
}
