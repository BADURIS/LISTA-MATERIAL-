using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Domain.Entities;
using LMaterial.Domain.Enums;
using LMaterial.Domain.Repositories;
using LMaterial.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace LMaterial.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _context;

    public UsuarioRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<Usuario?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => _context.Usuarios.FirstOrDefaultAsync(u => u.Username == username, ct);

    public async Task<IReadOnlyList<Usuario>> GetAtivosPorPerfilAsync(PerfilUsuario perfil, CancellationToken ct = default)
        => await _context.Usuarios.Where(u => u.Ativo && u.Perfil == perfil).ToListAsync(ct);

    public Task<bool> ExisteAsync(CancellationToken ct = default)
        => _context.Usuarios.AnyAsync(ct);

    public async Task AddAsync(Usuario usuario, CancellationToken ct = default)
        => await _context.Usuarios.AddAsync(usuario, ct);
}
