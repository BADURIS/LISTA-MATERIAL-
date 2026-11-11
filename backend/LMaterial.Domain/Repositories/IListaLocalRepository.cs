using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Domain.Entities;

namespace LMaterial.Domain.Repositories;

public interface IListaLocalRepository
{
    Task<ListaLocal?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<ListaLocal>> GetBySemanaIdAsync(Guid semanaId, CancellationToken ct = default);
    Task AddAsync(ListaLocal lista, CancellationToken ct = default);
    Task UpdateAsync(ListaLocal lista, CancellationToken ct = default);
}
