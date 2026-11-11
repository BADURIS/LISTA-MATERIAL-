using System;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Application.DTOs.Consumo;
using LMaterial.Application.Services.Consumo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LMaterial.API.Controllers;

// Relatórios de consumo (material que saiu do almoxarifado com retirada registrada)
[ApiController]
[Route("api/consumo")]
[Authorize]
public class ConsumoController : ControllerBase
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IConsumoService _consumoService;
    private readonly LMaterial.Application.Services.Relatorios.IRelatorioPdfService _relatorioPdfService;

    public ConsumoController(IConsumoService consumoService, LMaterial.Application.Services.Relatorios.IRelatorioPdfService relatorioPdfService)
    {
        _consumoService = consumoService ?? throw new ArgumentNullException(nameof(consumoService));
        _relatorioPdfService = relatorioPdfService ?? throw new ArgumentNullException(nameof(relatorioPdfService));
    }

    [HttpGet("periodo/pdf")]
    [Authorize(Roles = "Gestor,Supervisor")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PeriodoPdf([FromQuery] DateOnly dataInicio, [FromQuery] DateOnly dataFim, [FromQuery] Guid? localId, CancellationToken ct)
    {
        var (conteudo, nomeArquivo) = await _relatorioPdfService.GerarConsumoAsync(dataInicio, dataFim, localId, ct);
        return File(conteudo, "application/pdf", nomeArquivo);
    }

    // Consumo por material e por local num período (opcionalmente de um único local)
    [HttpGet("periodo")]
    [Authorize(Roles = "Gestor,Supervisor")]
    [ProducesResponseType(typeof(ConsumoPeriodoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConsumoPeriodoDto>> Periodo([FromQuery] DateOnly dataInicio, [FromQuery] DateOnly dataFim, [FromQuery] Guid? localId, CancellationToken ct)
    {
        var result = await _consumoService.ObterPorPeriodoAsync(dataInicio, dataFim, localId, ct);
        return Ok(result);
    }

    [HttpGet("periodo/planilha")]
    [Authorize(Roles = "Gestor,Supervisor")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PeriodoPlanilha([FromQuery] DateOnly dataInicio, [FromQuery] DateOnly dataFim, [FromQuery] Guid? localId, CancellationToken ct)
    {
        var (conteudo, nomeArquivo) = await _consumoService.ExportarPeriodoXlsxAsync(dataInicio, dataFim, localId, ct);
        return File(conteudo, XlsxContentType, nomeArquivo);
    }

    // Evolução semana a semana (todos os perfis)
    [HttpGet("semanal")]
    [ProducesResponseType(typeof(ConsumoSemanalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConsumoSemanalDto>> Semanal([FromQuery] int semanas = 8, CancellationToken ct = default)
    {
        var result = await _consumoService.ObterEvolucaoSemanalAsync(semanas, ct);
        return Ok(result);
    }
}
