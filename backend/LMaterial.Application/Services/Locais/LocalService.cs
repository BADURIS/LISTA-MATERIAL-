using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Application.DTOs.Common;
using LMaterial.Application.DTOs.Locais;
using LMaterial.Domain.Entities;
using LMaterial.Domain.Enums;
using LMaterial.Domain.Interfaces;
using LMaterial.Domain.Repositories;

namespace LMaterial.Application.Services.Locais;

public class LocalService : ILocalService
{
    private readonly ILocalRepository _localRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LocalService(ILocalRepository localRepository, IUnitOfWork unitOfWork)
    {
        _localRepository = localRepository ?? throw new ArgumentNullException(nameof(localRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<LocalDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var local = await _localRepository.GetByIdAsync(id, ct);
        return local == null ? null : MapToDto(local);
    }

    public async Task<IEnumerable<LocalDto>> GetAllAsync(CancellationToken ct = default)
    {
        var locais = await _localRepository.GetAllAsync(ct);
        return locais.Select(MapToDto);
    }

    public async Task<IEnumerable<LocalDto>> GetAtivosAsync(CancellationToken ct = default)
    {
        var locais = await _localRepository.GetAtivosAsync(ct);
        return locais.Select(MapToDto);
    }

    public async Task<PagedResult<LocalDto>> GetPagedAsync(PaginationParams pagination, CancellationToken ct = default)
    {
        var (items, totalCount) = await _localRepository.GetPagedAsync(pagination.PageIndex, pagination.PageSize, ct);
        var dtos = items.Select(MapToDto);
        return PagedResult<LocalDto>.Create(dtos, pagination.PageIndex, pagination.PageSize, totalCount);
    }

    public async Task<PagedResult<LocalDto>> GetAtivosPagedAsync(PaginationParams pagination, CancellationToken ct = default)
    {
        var (items, totalCount) = await _localRepository.GetAtivosPagedAsync(pagination.PageIndex, pagination.PageSize, ct);
        var dtos = items.Select(MapToDto);
        return PagedResult<LocalDto>.Create(dtos, pagination.PageIndex, pagination.PageSize, totalCount);
    }

    public async Task<LocalDto> CreateAsync(CreateLocalRequest request, CancellationToken ct = default)
    {
        TipoLocal tipo = TipoLocal.Andar;
        if (!string.IsNullOrWhiteSpace(request.Tipo) && !Enum.TryParse<TipoLocal>(request.Tipo, ignoreCase: true, out tipo))
        {
            throw new FluentValidation.ValidationException("Tipo de local inválido.");
        }

        var local = Local.Create(request.Nome, tipo, request.OrdemExibicao);

        await _localRepository.AddAsync(local, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToDto(local);
    }

    public async Task<LocalDto?> UpdateAsync(Guid id, UpdateLocalRequest request, CancellationToken ct = default)
    {
        var local = await _localRepository.GetByIdAsync(id, ct);
        if (local == null) return null;

        local.AtualizarNome(request.Nome);

        if (!string.IsNullOrWhiteSpace(request.Tipo))
        {
            if (!Enum.TryParse<TipoLocal>(request.Tipo, ignoreCase: true, out var tipo))
            {
                throw new FluentValidation.ValidationException("Tipo de local inválido.");
            }
            local.AlterarTipo(tipo);
        }

        local.AlterarOrdemExibicao(request.OrdemExibicao);

        if (request.Ativo)
            local.Ativar();
        else
            local.Inativar();

        await _localRepository.UpdateAsync(local, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToDto(local);
    }

    private static LocalDto MapToDto(Local local)
    {
        return new LocalDto(
            local.Id,
            local.Nome,
            local.Ativo,
            local.Tipo.ToString(),
            local.OrdemExibicao
        );
    }
}
