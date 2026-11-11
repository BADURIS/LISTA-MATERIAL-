using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Domain.Entities;
using LMaterial.Domain.Repositories;
using LMaterial.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace LMaterial.Infrastructure.Repositories;

public class ListaLocalRepository : IListaLocalRepository
{
    private readonly AppDbContext _context;

    public ListaLocalRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ListaLocal?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.ListasLocais
            .Include(l => l.Itens)
                .ThenInclude(i => i.Material)
            .Include(l => l.Local)
            .FirstOrDefaultAsync(l => l.Id == id, ct);
    }

    public async Task<IEnumerable<ListaLocal>> GetBySemanaIdAsync(Guid semanaId, CancellationToken ct = default)
    {
        return await _context.ListasLocais
            .Include(l => l.Itens)
                .ThenInclude(i => i.Material)
            .Include(l => l.Local)
            .Where(l => l.SemanaListaId == semanaId)
            .ToListAsync(ct);
    }

    public async Task AddAsync(ListaLocal lista, CancellationToken ct = default)
    {
        await _context.ListasLocais.AddAsync(lista, ct);
    }

    public Task UpdateAsync(ListaLocal lista, CancellationToken ct = default)
    {
        _context.ListasLocais.Update(lista);
        return Task.CompletedTask;
    }
}
