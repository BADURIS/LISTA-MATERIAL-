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

    private const string PdfContentType = "application/pdf";

    private readonly ISemanaListaService _semanaListaService;
    private readonly IPlanilhaSemanaService _planilhaSemanaService;
    private readonly LMaterial.Application.Services.Relatorios.IRelatorioPdfService _relatorioPdfService;

    public SemanasListaController(
        ISemanaListaService semanaListaService,
        IPlanilhaSemanaService planilhaSemanaService,
        LMaterial.Application.Services.Relatorios.IRelatorioPdfService relatorioPdfService)
    {
        _semanaListaService = semanaListaService ?? throw new ArgumentNullException(nameof(semanaListaService));
        _planilhaSemanaService = planilhaSemanaService ?? throw new ArgumentNullException(nameof(planilhaSemanaService));
        _relatorioPdfService = relatorioPdfService ?? throw new ArgumentNullException(nameof(relatorioPdfService));
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

    // Pedido completo de um local (substitui o anterior). Usado pela tela de preenchimento.
    [HttpPut("{id:guid}/locais/{localId:guid}/itens")]
    [Authorize(Roles = "Supervisor,Coordenador")]
    [ProducesResponseType(typeof(SemanaListaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SemanaListaDto>> PreencherLocal(Guid id, Guid localId, [FromBody] PreencherLocalRequest request, CancellationToken ct)
    {
        var result = await _semanaListaService.PreencherLocalAsync(id, localId, request, ct);
        return Ok(result);
    }

    // Marca (ou desmarca) o local como sem pedido nesta semana
    [HttpPut("{id:guid}/locais/{localId:guid}/sem-pedido")]
    [Authorize(Roles = "Supervisor,Coordenador")]
    [ProducesResponseType(typeof(SemanaListaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SemanaListaDto>> DefinirSemPedido(Guid id, Guid localId, [FromBody] DefinirSemPedidoRequest request, CancellationToken ct)
    {
        var result = await _semanaListaService.DefinirSemPedidoAsync(id, localId, request, ct);
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
    [Authorize(Roles = "Almoxarife")]
    [ProducesResponseType(typeof(SemanaListaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SemanaListaDto>> ConfirmarRecebimento(Guid id, [FromBody] ConfirmarRecebimentoRequest? request, CancellationToken ct)
    {
        var req = request ?? new ConfirmarRecebimentoRequest();
        var result = await _semanaListaService.ConfirmarRecebimentoAsync(id, req, ct);
        return Ok(result);
    }

    // Retirada registrada pelo Almoxarife no balcão: o nome do responsável que retirou e a data/hora
    // formam a assinatura digital da lista
    [HttpPost("{id:guid}/registrar-retirada")]
    [Authorize(Roles = "Almoxarife")]
    [ProducesResponseType(typeof(SemanaListaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SemanaListaDto>> RegistrarRetirada(Guid id, [FromBody] RegistrarRetiradaRequest request, CancellationToken ct)
    {
        var result = await _semanaListaService.RegistrarRetiradaAsync(id, request, User.ObterUsuarioId(), ct);
        return Ok(result);
    }

    // Histórico de semanas (todos os perfis consultam)
    [HttpGet("historico")]
    [ProducesResponseType(typeof(IReadOnlyList<SemanaResumoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SemanaResumoDto>>> Historico(CancellationToken ct)
    {
        var result = await _semanaListaService.ListarHistoricoAsync(ct);
        return Ok(result);
    }

    // Visão consolidada da semana (MATERIAL | UNIDADE | SAÍDA)
    [HttpGet("{id:guid}/resumo")]
    [Authorize(Roles = "Gestor,Almoxarife,Supervisor")]
    [ProducesResponseType(typeof(IReadOnlyList<ResumoMaterialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ResumoMaterialDto>>> Resumo(Guid id, CancellationToken ct)
    {
        var result = await _planilhaSemanaService.GerarResumoAsync(id, ct);
        return Ok(result);
    }

    // Exportação da planilha semanal em XLSX
    [HttpGet("{id:guid}/planilha")]
    [Authorize(Roles = "Gestor,Almoxarife,Supervisor")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportarPlanilha(Guid id, CancellationToken ct)
    {
        var (conteudo, nomeArquivo) = await _planilhaSemanaService.GerarXlsxAsync(id, ct);
        return File(conteudo, XlsxContentType, nomeArquivo);
    }

    // Exportação da lista de um local em XLSX (todos os perfis)
    [HttpGet("{id:guid}/listas/{listaLocalId:guid}/planilha")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportarPlanilhaLocal(Guid id, Guid listaLocalId, CancellationToken ct)
    {
        var (conteudo, nomeArquivo) = await _planilhaSemanaService.GerarXlsxLocalAsync(id, listaLocalId, ct);
        return File(conteudo, XlsxContentType, nomeArquivo);
    }

    // Levantamento da semana em PDF (saída por material e situação de cada local)
    [HttpGet("{id:guid}/pdf")]
    [Authorize(Roles = "Gestor,Almoxarife,Supervisor")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportarPdf(Guid id, CancellationToken ct)
    {
        var (conteudo, nomeArquivo) = await _relatorioPdfService.GerarSemanaAsync(id, ct);
        return File(conteudo, PdfContentType, nomeArquivo);
    }

    // Lista de um local em PDF, com dados da retirada e espaço para assinatura (todos os perfis)
    [HttpGet("{id:guid}/listas/{listaLocalId:guid}/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportarPdfLocal(Guid id, Guid listaLocalId, CancellationToken ct)
    {
        var (conteudo, nomeArquivo) = await _relatorioPdfService.GerarListaLocalAsync(id, listaLocalId, ct);
        return File(conteudo, PdfContentType, nomeArquivo);
    }
}
