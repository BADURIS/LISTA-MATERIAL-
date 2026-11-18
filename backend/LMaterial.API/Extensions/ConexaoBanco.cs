using System;
using Npgsql;

namespace LMaterial.API.Extensions;

// Aceita a string de conexão nos dois formatos:
// - o do Npgsql: "Host=...;Port=...;Database=...;Username=...;Password=..."
// - a URI que o Supabase e o Render mostram: "postgresql://usuario:senha@host:porta/banco"
public static class ConexaoBanco
{
    public static string? Normalizar(string? conexao)
    {
        if (string.IsNullOrWhiteSpace(conexao))
            return conexao;

        var texto = conexao.Trim();
        if (!texto.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !texto.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return texto;

        var uri = new Uri(texto);
        var credenciais = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')) is { Length: > 0 } banco ? banco : "postgres",
            Username = Uri.UnescapeDataString(credenciais[0]),
            Password = credenciais.Length > 1 ? Uri.UnescapeDataString(credenciais[1]) : null,
        };

        // Banco remoto (Supabase, Render): conexão sempre criptografada
        if (!uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) && uri.Host != "127.0.0.1")
            builder.SslMode = SslMode.Require;

        return builder.ConnectionString;
    }
}
