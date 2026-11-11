using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Application.DTOs.Common;
using LMaterial.Application.DTOs.Locais;
using LMaterial.Application.Services.Locais;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LMaterial.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LocaisController : ControllerBase
{
    private readonly ILocalService _localService;

    public LocaisController(ILocalService localService)
    {
        _localService = localService ?? throw new ArgumentNullException(nameof(localService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LocalDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LocalDto>>> GetAll([FromQuery] PaginationParams pagination, CancellationToken ct)
    {
        var result = await _localService.GetPagedAsync(pagination, ct);
        return Ok(result);
    }

    [HttpGet("ativos")]
    [ProducesResponseType(typeof(PagedResult<LocalDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LocalDto>>> GetAtivos([FromQuery] PaginationParams pagination, CancellationToken ct)
    {
        var result = await _localService.GetAtivosPagedAsync(pagination, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LocalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocalDto>> GetById(Guid id, CancellationToken ct)
    {
        var local = await _localService.GetByIdAsync(id, ct);
        if (local == null)
            return NotFound();

        return Ok(local);
    }

    [HttpPost]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(typeof(LocalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LocalDto>> Create([FromBody] CreateLocalRequest request, CancellationToken ct)
    {
        var result = await _localService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(typeof(LocalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LocalDto>> Update(Guid id, [FromBody] UpdateLocalRequest request, CancellationToken ct)
    {
        var result = await _localService.UpdateAsync(id, request, ct);
        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
