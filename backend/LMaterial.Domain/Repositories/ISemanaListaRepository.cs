using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Domain.Entities;

namespace LMaterial.Domain.Repositories;

public interface ISemanaListaRepository
{
    Task<SemanaLista?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<SemanaLista>> GetAllAsync(CancellationToken ct = default);
    Task<SemanaLista?> GetSemanaAtualAsync(CancellationToken ct = default);
    Task AddAsync(SemanaLista semana, CancellationToken ct = default);
    Task UpdateAsync(SemanaLista semana, CancellationToken ct = default);
}
