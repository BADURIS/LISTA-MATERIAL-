using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Domain.Entities;

namespace LMaterial.Domain.Repositories;

public interface IMaterialRepository
{
    Task<Material?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Material>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<Material>> GetAtivosAsync(CancellationToken ct = default);
    Task<(IEnumerable<Material> Items, long TotalCount)> GetPagedAsync(int pageIndex, int pageSize, CancellationToken ct = default);
    Task<(IEnumerable<Material> Items, long TotalCount)> GetAtivosPagedAsync(int pageIndex, int pageSize, CancellationToken ct = default);
    Task AddAsync(Material material, CancellationToken ct = default);
    Task UpdateAsync(Material material, CancellationToken ct = default);
    Task<bool> EstaEmUsoAsync(Guid id, CancellationToken ct = default);
    void Remover(Material material);
}
