using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Application.DTOs.Common;
using LMaterial.Application.DTOs.Materiais;
using LMaterial.Domain.Entities;
using LMaterial.Domain.Enums;
using LMaterial.Domain.Interfaces;
using LMaterial.Domain.Repositories;

namespace LMaterial.Application.Services.Materiais;

public class MaterialService : IMaterialService
{
    private readonly IMaterialRepository _materialRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MaterialService(IMaterialRepository materialRepository, IUnitOfWork unitOfWork)
    {
        _materialRepository = materialRepository ?? throw new ArgumentNullException(nameof(materialRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<MaterialDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var material = await _materialRepository.GetByIdAsync(id, ct);
        return material == null ? null : MapToDto(material);
    }

    public async Task<IEnumerable<MaterialDto>> GetAllAsync(CancellationToken ct = default)
    {
        var materiais = await _materialRepository.GetAllAsync(ct);
        return materiais.Select(MapToDto);
    }

    public async Task<IEnumerable<MaterialDto>> GetAtivosAsync(CancellationToken ct = default)
    {
        var materiais = await _materialRepository.GetAtivosAsync(ct);
        return materiais.Select(MapToDto);
    }

    public async Task<PagedResult<MaterialDto>> GetPagedAsync(PaginationParams pagination, CancellationToken ct = default)
    {
        var (items, totalCount) = await _materialRepository.GetPagedAsync(pagination.PageIndex, pagination.PageSize, ct);
        var dtos = items.Select(MapToDto);
        return PagedResult<MaterialDto>.Create(dtos, pagination.PageIndex, pagination.PageSize, totalCount);
    }

    public async Task<PagedResult<MaterialDto>> GetAtivosPagedAsync(PaginationParams pagination, CancellationToken ct = default)
    {
        var (items, totalCount) = await _materialRepository.GetAtivosPagedAsync(pagination.PageIndex, pagination.PageSize, ct);
        var dtos = items.Select(MapToDto);
        return PagedResult<MaterialDto>.Create(dtos, pagination.PageIndex, pagination.PageSize, totalCount);
    }

    public async Task<MaterialDto> CreateAsync(CreateMaterialRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<Unidade>(request.Unidade, ignoreCase: true, out var unidade))
        {
            throw new FluentValidation.ValidationException("Unidade de medida inválida.");
        }

        var material = Material.Create(request.Nome, unidade, request.OrdemExibicao);

        await _materialRepository.AddAsync(material, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToDto(material);
    }

    public async Task<MaterialDto?> UpdateAsync(Guid id, UpdateMaterialRequest request, CancellationToken ct = default)
    {
        var material = await _materialRepository.GetByIdAsync(id, ct);
        if (material == null) return null;

        if (!Enum.TryParse<Unidade>(request.Unidade, ignoreCase: true, out var unidade))
        {
            throw new FluentValidation.ValidationException("Unidade de medida inválida.");
        }

        material.AtualizarNome(request.Nome);
        material.AlterarUnidade(unidade);
        material.AlterarOrdemExibicao(request.OrdemExibicao);

        if (request.Ativo)
            material.Ativar();
        else
            material.Inativar();

        await _materialRepository.UpdateAsync(material, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToDto(material);
    }

    // Material nunca usado em pedido é apagado; o que já está no histórico é só inativado
    public async Task<ExcluirMaterialResultado?> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var material = await _materialRepository.GetByIdAsync(id, ct);
        if (material == null) return null;

        if (await _materialRepository.EstaEmUsoAsync(id, ct))
        {
            material.Inativar();
            await _materialRepository.UpdateAsync(material, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return new ExcluirMaterialResultado(Excluido: false, Inativado: true, MapToDto(material));
        }

        _materialRepository.Remover(material);
        await _unitOfWork.SaveChangesAsync(ct);
        return new ExcluirMaterialResultado(Excluido: true, Inativado: false, null);
    }

    private static MaterialDto MapToDto(Material material)
    {
        return new MaterialDto(
            material.Id,
            material.Nome,
            material.Unidade.ToString(),
            material.OrdemExibicao,
            Ativo: material.Ativo
        );
    }
}
