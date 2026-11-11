using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Application.DTOs.Common;
using LMaterial.Application.DTOs.Locais;

namespace LMaterial.Application.Services.Locais;

public interface ILocalService
{
    Task<LocalDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<LocalDto>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<LocalDto>> GetAtivosAsync(CancellationToken ct = default);
    Task<PagedResult<LocalDto>> GetPagedAsync(PaginationParams pagination, CancellationToken ct = default);
    Task<PagedResult<LocalDto>> GetAtivosPagedAsync(PaginationParams pagination, CancellationToken ct = default);
    Task<LocalDto> CreateAsync(CreateLocalRequest request, CancellationToken ct = default);
    Task<LocalDto?> UpdateAsync(Guid id, UpdateLocalRequest request, CancellationToken ct = default);
}
