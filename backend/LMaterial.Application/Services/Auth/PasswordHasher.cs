namespace LMaterial.Application.Services.Auth;

// Senhas são armazenadas somente como hash BCrypt (fator de custo 12)
public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string senha) => BCrypt.Net.BCrypt.HashPassword(senha, WorkFactor);

    public bool Verify(string senha, string hash)
    {
        if (string.IsNullOrEmpty(senha) || string.IsNullOrEmpty(hash))
            return false;

        return BCrypt.Net.BCrypt.Verify(senha, hash);
    }
}
