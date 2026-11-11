using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.API.Extensions;
using LMaterial.Application.DTOs.SemanasLista;
using LMaterial.Application.Services.Planilhas;
using LMaterial.Application.Services.SemanasLista;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LMaterial.API.Controllers;

// Perfis: Supervisor monta e envia a semana; Coordenador preenche itens, confirma recebimento e registra retirada;
// Almoxarife registra a separação; Gestor consulta e exporta a planilha.
[ApiController]
[Route("api/semanas-lista")]
[Authorize]
public class SemanasListaController : ControllerBase
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly ISemanaListaService _semanaListaService;
    private readonly IPlanilhaSemanaService _planilhaSemanaService;

    public SemanasListaController(ISemanaListaService semanaListaService, IPlanilhaSemanaService planilhaSemanaService)
    {
        _semanaListaService = semanaListaService ?? throw new ArgumentNullException(nameof(semanaListaService));
        _planilhaSemanaService = planilhaSemanaService ?? throw new ArgumentNullException(nameof(planilhaSemanaService));
    }

    [HttpPost]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(typeof(SemanaListaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SemanaListaDto>> CriarSemana([FromBody] CriarSemanaRequest request, CancellationToken ct)
    {
        var result = await _semanaListaService.CriarSemanaAsync(request, User.ObterUsuarioId(), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("atual")]
    [ProducesResponseType(typeof(SemanaListaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SemanaListaDto>> GetSemanaAtual(CancellationToken ct)
    {
        var result = await _semanaListaService.GetSemanaAtualAsync(ct);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SemanaListaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SemanaListaDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _semanaListaService.GetByIdAsync(id, ct);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost("{id:guid}/locais")]
    [Authorize(Roles = "Supervisor,Coordenador")]
    [ProducesResponseType(typeof(SemanaListaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SemanaListaDto>> AdicionarLocal(Guid id, [FromBody] AdicionarLocalRequest request, CancellationToken ct)
    {
        var result = await _semanaListaService.AdicionarLocalAsync(id, request, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/itens")]
    [Authorize(Roles = "Supervisor,Coordenador")]
    [ProducesResponseType(typeof(SemanaListaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SemanaListaDto>> AdicionarItem(Guid id, [FromBody] AdicionarItemRequest request, CancellationToken ct)
    {
        var result = await _semanaListaService.AdicionarItemAsync(id, request, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/enviar-separacao")]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(typeof(SemanaListaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SemanaListaDto>> EnviarParaSeparacao(Guid id, CancellationToken ct)
    {
        var result = await _semanaListaService.EnviarParaSeparacaoAsync(id, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/registrar-separacao")]
    [Authorize(Roles = "Almoxarife")]
    [ProducesResponseType(typeof(SemanaListaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SemanaListaDto>> RegistrarSeparacao(Guid id, [FromBody] RegistrarSeparacaoRequest request, CancellationToken ct)
    {
        var result = await _semanaListaService.RegistrarSeparacaoAsync(id, request, User.ObterUsuarioId(), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/confirmar-recebimento")]
    [Authorize(Roles = "Coordenador")]
    [ProducesResponseType(typeof(SemanaListaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SemanaListaDto>> ConfirmarRecebimento(Guid id, [FromBody] ConfirmarRecebimentoRequest? request, CancellationToken ct)
    {
        var req = request ?? new ConfirmarRecebimentoRequest();
        var result = await _semanaListaService.ConfirmarRecebimentoAsync(id, req, ct);
        return Ok(result);
    }

    // Retirada registrada pelo Coordenador; o nome informado e a data/hora formam a assinatura digital da lista
    [HttpPost("{id:guid}/registrar-retirada")]
    [Authorize(Roles = "Coordenador")]
    [ProducesResponseType(typeof(SemanaListaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SemanaListaDto>> RegistrarRetirada(Guid id, [FromBody] RegistrarRetiradaRequest request, CancellationToken ct)
    {
        var result = await _semanaListaService.RegistrarRetiradaAsync(id, request, User.ObterUsuarioId(), ct);
        return Ok(result);
    }

    // Histórico de semanas, para o Gestor escolher qual planilha consultar ou exportar
    [HttpGet("historico")]
    [Authorize(Roles = "Gestor")]
    [ProducesResponseType(typeof(IReadOnlyList<SemanaResumoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SemanaResumoDto>>> Historico(CancellationToken ct)
    {
        var result = await _semanaListaService.ListarHistoricoAsync(ct);
        return Ok(result);
    }

    // Visão consolidada da semana (MATERIAL | UNIDADE | SAÍDA), para o Gestor
    [HttpGet("{id:guid}/resumo")]
    [Authorize(Roles = "Gestor")]
    [ProducesResponseType(typeof(IReadOnlyList<ResumoMaterialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ResumoMaterialDto>>> Resumo(Guid id, CancellationToken ct)
    {
        var result = await _planilhaSemanaService.GerarResumoAsync(id, ct);
        return Ok(result);
    }

    // Exportação da planilha semanal em XLSX, para o Gestor
    [HttpGet("{id:guid}/planilha")]
    [Authorize(Roles = "Gestor")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportarPlanilha(Guid id, CancellationToken ct)
    {
        var (conteudo, nomeArquivo) = await _planilhaSemanaService.GerarXlsxAsync(id, ct);
        return File(conteudo, XlsxContentType, nomeArquivo);
    }
}
