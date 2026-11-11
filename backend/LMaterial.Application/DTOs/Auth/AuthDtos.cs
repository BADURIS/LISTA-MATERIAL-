namespace LMaterial.Application.DTOs.Auth;

public record LoginRequest(string Username, string Password);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    string Username,
    string NomeCompleto,
    string Role
);

public record RefreshTokenRequest(
    string AccessToken,
    string RefreshToken
);
