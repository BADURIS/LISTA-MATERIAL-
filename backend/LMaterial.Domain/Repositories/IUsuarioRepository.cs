using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Domain.Entities;
using LMaterial.Domain.Enums;

namespace LMaterial.Domain.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Usuario?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<IReadOnlyList<Usuario>> GetAtivosPorPerfilAsync(PerfilUsuario perfil, CancellationToken ct = default);
    Task<bool> ExisteAsync(CancellationToken ct = default);
    Task AddAsync(Usuario usuario, CancellationToken ct = default);
}
