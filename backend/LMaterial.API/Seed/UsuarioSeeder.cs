using System;
using System.Threading.Tasks;
using LMaterial.Application.Services.Auth;
using LMaterial.Domain.Entities;
using LMaterial.Domain.Enums;
using LMaterial.Domain.Interfaces;
using LMaterial.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LMaterial.API.Seed;

// Cria os usuários iniciais definidos em "SeedUsuarios". A senha de cada um deve vir de user-secrets
// ou variável de ambiente (SeedUsuarios:<usuario>:Password). Entradas sem senha são ignoradas.
// A criação é idempotente; para usuários que já existem só a senha é sincronizada com a configuração.
public static class UsuarioSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        var usuarioRepository = services.GetRequiredService<IUsuarioRepository>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();

        var alterou = false;

        foreach (var entrada in configuration.GetSection("SeedUsuarios").GetChildren())
        {
            var username = entrada.Key;
            var senha = entrada["Password"];

            if (string.IsNullOrWhiteSpace(senha))
            {
                logger.LogWarning("Usuário '{Username}' não criado: defina SeedUsuarios:{Username}:Password em user-secrets.", username, username);
                continue;
            }

            if (!Enum.TryParse<PerfilUsuario>(entrada["Perfil"], ignoreCase: true, out var perfil))
            {
                logger.LogWarning("Usuário '{Username}' não criado: perfil inválido.", username);
                continue;
            }

            // Usuário já existente: a senha configurada é a vigente. Se mudou, o hash é atualizado.
            var existente = await usuarioRepository.GetByUsernameAsync(username);
            if (existente != null)
            {
                if (!passwordHasher.Verify(senha, existente.SenhaHash))
                {
                    existente.AlterarSenhaHash(passwordHasher.Hash(senha));
                    alterou = true;
                    logger.LogInformation("Senha do usuário '{Username}' atualizada.", username);
                }
                continue;
            }

            var nomeCompleto = entrada["NomeCompleto"] ?? username;
            await usuarioRepository.AddAsync(Usuario.Create(username, nomeCompleto, passwordHasher.Hash(senha), perfil));
            alterou = true;

            logger.LogInformation("Usuário '{Username}' ({Perfil}) criado.", username, perfil);
        }

        if (alterou)
        {
            await unitOfWork.SaveChangesAsync();
        }
    }
}
