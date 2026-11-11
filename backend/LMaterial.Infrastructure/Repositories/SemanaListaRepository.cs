using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Domain.Entities;
using LMaterial.Domain.Enums;
using LMaterial.Domain.Repositories;
using LMaterial.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace LMaterial.Infrastructure.Repositories;

public class SemanaListaRepository : ISemanaListaRepository
{
    private readonly AppDbContext _context;

    public SemanaListaRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<SemanaLista?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.SemanasListas
            .Include(s => s.ListasLocais)
                .ThenInclude(l => l.Itens)
            .Include(s => s.ListasLocais)
                .ThenInclude(l => l.Local)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<IEnumerable<SemanaLista>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.SemanasListas
            .Include(s => s.ListasLocais)
                .ThenInclude(l => l.Itens)
            .OrderByDescending(s => s.DataInicio)
            .ToListAsync(ct);
    }

    public async Task<SemanaLista?> GetSemanaAtualAsync(CancellationToken ct = default)
    {
        return await _context.SemanasListas
            .Include(s => s.ListasLocais)
                .ThenInclude(l => l.Itens)
            .Include(s => s.ListasLocais)
                .ThenInclude(l => l.Local)
            .Where(s => s.Status != StatusSemanaLista.Finalizada)
            .OrderByDescending(s => s.DataCriacao)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(SemanaLista semana, CancellationToken ct = default)
    {
        await _context.SemanasListas.AddAsync(semana, ct);
    }

    // A semana já vem rastreada pelo contexto. Chamar Update() marcaria as listas locais novas como modificadas
    // (em vez de inseridas), o que gera conflito de concorrência. O SaveChanges detecta as alterações sozinho.
    public Task UpdateAsync(SemanaLista semana, CancellationToken ct = default)
    {
        if (_context.Entry(semana).State == EntityState.Detached)
        {
            _context.SemanasListas.Attach(semana);
        }

        return Task.CompletedTask;
    }
}
