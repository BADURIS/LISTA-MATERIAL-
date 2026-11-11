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

public class MaterialRepository : IMaterialRepository
{
    private readonly AppDbContext _context;

    public MaterialRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Material?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Materiais
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<IEnumerable<Material>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Materiais
            .AsNoTracking()
            .OrderBy(m => m.OrdemExibicao)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Material>> GetAtivosAsync(CancellationToken ct = default)
    {
        return await _context.Materiais
            .AsNoTracking()
            .Where(m => m.Ativo)
            .OrderBy(m => m.OrdemExibicao)
            .ToListAsync(ct);
    }

    public async Task<(IEnumerable<Material> Items, long TotalCount)> GetPagedAsync(int pageIndex, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Materiais.AsNoTracking();
        var totalCount = await query.LongCountAsync(ct);

        var items = await query
            .OrderBy(m => m.OrdemExibicao)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Material> Items, long TotalCount)> GetAtivosPagedAsync(int pageIndex, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Materiais.AsNoTracking().Where(m => m.Ativo);
        var totalCount = await query.LongCountAsync(ct);

        var items = await query
            .OrderBy(m => m.OrdemExibicao)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task AddAsync(Material material, CancellationToken ct = default)
    {
        await _context.Materiais.AddAsync(material, ct);
    }

    public Task UpdateAsync(Material material, CancellationToken ct = default)
    {
        _context.Materiais.Update(material);
        return Task.CompletedTask;
    }
}
