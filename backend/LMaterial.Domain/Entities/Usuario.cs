using System;
using LMaterial.Domain.Enums;
using LMaterial.Domain.Exceptions;

namespace LMaterial.Domain.Entities;

public class Usuario
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string NomeCompleto { get; private set; } = string.Empty;
    public string SenhaHash { get; private set; } = string.Empty;
    public PerfilUsuario Perfil { get; private set; }
    public bool Ativo { get; private set; }

    // Refresh token armazenado apenas como hash (SHA-256), nunca em texto puro
    public string? RefreshTokenHash { get; private set; }
    public DateTime? RefreshTokenExpiracao { get; private set; }

    private Usuario()
    {
    }

    private Usuario(Guid id, string username, string nomeCompleto, string senhaHash, PerfilUsuario perfil, bool ativo)
    {
        if (id == Guid.Empty)
            throw new DomainException("O Id do usuário não pode ser vazio.");

        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("O nome de usuário é obrigatório.");

        if (string.IsNullOrWhiteSpace(nomeCompleto))
            throw new DomainException("O nome completo do usuário é obrigatório.");

        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new DomainException("O hash da senha é obrigatório.");

        Id = id;
        Username = username.Trim();
        NomeCompleto = nomeCompleto.Trim();
        SenhaHash = senhaHash;
        Perfil = perfil;
        Ativo = ativo;
    }

    public static Usuario Create(string username, string nomeCompleto, string senhaHash, PerfilUsuario perfil)
    {
        return new Usuario(Guid.NewGuid(), username, nomeCompleto, senhaHash, perfil, ativo: true);
    }

    public static Usuario CreateWithId(Guid id, string username, string nomeCompleto, string senhaHash, PerfilUsuario perfil, bool ativo)
    {
        return new Usuario(id, username, nomeCompleto, senhaHash, perfil, ativo);
    }

    public void DefinirRefreshToken(string refreshTokenHash, DateTime expiracao)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenHash))
            throw new DomainException("O hash do refresh token é obrigatório.");

        RefreshTokenHash = refreshTokenHash;
        RefreshTokenExpiracao = expiracao;
    }

    public void RevogarRefreshToken()
    {
        RefreshTokenHash = null;
        RefreshTokenExpiracao = null;
    }

    public void Inativar() => Ativo = false;

    // Troca a senha e derruba as sessões abertas (o refresh token antigo deixa de valer)
    public void AlterarSenhaHash(string novaSenhaHash)
    {
        if (string.IsNullOrWhiteSpace(novaSenhaHash))
            throw new DomainException("O hash da senha é obrigatório.");

        SenhaHash = novaSenhaHash;
        RevogarRefreshToken();
    }
}
