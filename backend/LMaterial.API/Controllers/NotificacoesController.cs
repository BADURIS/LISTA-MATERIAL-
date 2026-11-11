using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.API.Extensions;
using LMaterial.Application.DTOs.Notificacoes;
using LMaterial.Application.Services.Notificacoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LMaterial.API.Controllers;

[ApiController]
[Route("api/notificacoes")]
[Authorize]
public class NotificacoesController : ControllerBase
{
    private readonly INotificacaoService _notificacaoService;

    public NotificacoesController(INotificacaoService notificacaoService)
    {
        _notificacaoService = notificacaoService ?? throw new ArgumentNullException(nameof(notificacaoService));
    }

    // Lista somente as notificações do usuário autenticado
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificacaoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificacaoDto>>> Listar([FromQuery] bool somenteNaoLidas, CancellationToken ct)
    {
        var result = await _notificacaoService.ListarAsync(User.ObterUsuarioId(), somenteNaoLidas, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/lida")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MarcarComoLida(Guid id, CancellationToken ct)
    {
        await _notificacaoService.MarcarComoLidaAsync(User.ObterUsuarioId(), id, ct);
        return NoContent();
    }
}
