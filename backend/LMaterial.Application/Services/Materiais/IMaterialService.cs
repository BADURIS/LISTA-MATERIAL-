using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Application.DTOs.Common;
using LMaterial.Application.DTOs.Materiais;

namespace LMaterial.Application.Services.Materiais;

public interface IMaterialService
{
    Task<MaterialDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<MaterialDto>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<MaterialDto>> GetAtivosAsync(CancellationToken ct = default);
    Task<PagedResult<MaterialDto>> GetPagedAsync(PaginationParams pagination, CancellationToken ct = default);
    Task<PagedResult<MaterialDto>> GetAtivosPagedAsync(PaginationParams pagination, CancellationToken ct = default);
    Task<MaterialDto> CreateAsync(CreateMaterialRequest request, CancellationToken ct = default);
    Task<MaterialDto?> UpdateAsync(Guid id, UpdateMaterialRequest request, CancellationToken ct = default);
}
