using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Domain.Entities;

namespace LMaterial.Domain.Repositories;

public interface ILocalRepository
{
    Task<Local?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Local>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<Local>> GetAtivosAsync(CancellationToken ct = default);
    Task<(IEnumerable<Local> Items, long TotalCount)> GetPagedAsync(int pageIndex, int pageSize, CancellationToken ct = default);
    Task<(IEnumerable<Local> Items, long TotalCount)> GetAtivosPagedAsync(int pageIndex, int pageSize, CancellationToken ct = default);
    Task AddAsync(Local local, CancellationToken ct = default);
    Task UpdateAsync(Local local, CancellationToken ct = default);
}
