using System;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Domain.Interfaces;
using LMaterial.Infrastructure.Context;

namespace LMaterial.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}
