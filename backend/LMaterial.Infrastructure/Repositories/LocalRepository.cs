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

public class LocalRepository : ILocalRepository
{
    private readonly AppDbContext _context;

    public LocalRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Local?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Locais
            .FirstOrDefaultAsync(l => l.Id == id, ct);
    }

    public async Task<IEnumerable<Local>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Locais
            .AsNoTracking()
            .OrderBy(l => l.OrdemExibicao)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Local>> GetAtivosAsync(CancellationToken ct = default)
    {
        return await _context.Locais
            .AsNoTracking()
            .Where(l => l.Ativo)
            .OrderBy(l => l.OrdemExibicao)
            .ToListAsync(ct);
    }

    public async Task<(IEnumerable<Local> Items, long TotalCount)> GetPagedAsync(int pageIndex, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Locais.AsNoTracking();
        var totalCount = await query.LongCountAsync(ct);

        var items = await query
            .OrderBy(l => l.OrdemExibicao)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Local> Items, long TotalCount)> GetAtivosPagedAsync(int pageIndex, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Locais.AsNoTracking().Where(l => l.Ativo);
        var totalCount = await query.LongCountAsync(ct);

        var items = await query
            .OrderBy(l => l.OrdemExibicao)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task AddAsync(Local local, CancellationToken ct = default)
    {
        await _context.Locais.AddAsync(local, ct);
    }

    public Task UpdateAsync(Local local, CancellationToken ct = default)
    {
        _context.Locais.Update(local);
        return Task.CompletedTask;
    }
}
