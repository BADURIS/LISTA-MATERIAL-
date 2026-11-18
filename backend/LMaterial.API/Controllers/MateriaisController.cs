using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Application.DTOs.Common;
using LMaterial.Application.DTOs.Materiais;
using LMaterial.Application.Services.Materiais;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LMaterial.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MateriaisController : ControllerBase
{
    private readonly IMaterialService _materialService;

    public MateriaisController(IMaterialService materialService)
    {
        _materialService = materialService ?? throw new ArgumentNullException(nameof(materialService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MaterialDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MaterialDto>>> GetAll([FromQuery] PaginationParams pagination, CancellationToken ct)
    {
        var result = await _materialService.GetPagedAsync(pagination, ct);
        return Ok(result);
    }

    [HttpGet("ativos")]
    [ProducesResponseType(typeof(PagedResult<MaterialDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MaterialDto>>> GetAtivos([FromQuery] PaginationParams pagination, CancellationToken ct)
    {
        var result = await _materialService.GetAtivosPagedAsync(pagination, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MaterialDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaterialDto>> GetById(Guid id, CancellationToken ct)
    {
        var material = await _materialService.GetByIdAsync(id, ct);
        if (material == null)
            return NotFound();

        return Ok(material);
    }

    [HttpPost]
    [Authorize(Roles = "Almoxarife")]
    [ProducesResponseType(typeof(MaterialDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MaterialDto>> Create([FromBody] CreateMaterialRequest request, CancellationToken ct)
    {
        var result = await _materialService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Almoxarife")]
    [ProducesResponseType(typeof(MaterialDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MaterialDto>> Update(Guid id, [FromBody] UpdateMaterialRequest request, CancellationToken ct)
    {
        var result = await _materialService.UpdateAsync(id, request, ct);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    // Apaga o material; se ele já foi usado em algum pedido, apenas o inativa para preservar o histórico
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Almoxarife")]
    [ProducesResponseType(typeof(ExcluirMaterialResultado), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExcluirMaterialResultado>> Delete(Guid id, CancellationToken ct)
    {
        var result = await _materialService.DeleteAsync(id, ct);
        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
