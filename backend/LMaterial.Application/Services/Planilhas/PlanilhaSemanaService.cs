using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using LMaterial.Application.DTOs.SemanasLista;
using LMaterial.Domain.Exceptions;
using LMaterial.Domain.Repositories;

namespace LMaterial.Application.Services.Planilhas;

public class PlanilhaSemanaService : IPlanilhaSemanaService
{
    private readonly ISemanaListaRepository _semanaListaRepository;
    private readonly IMaterialRepository _materialRepository;

    public PlanilhaSemanaService(ISemanaListaRepository semanaListaRepository, IMaterialRepository materialRepository)
    {
        _semanaListaRepository = semanaListaRepository ?? throw new ArgumentNullException(nameof(semanaListaRepository));
        _materialRepository = materialRepository ?? throw new ArgumentNullException(nameof(materialRepository));
    }

    public async Task<IReadOnlyList<ResumoMaterialDto>> GerarResumoAsync(Guid semanaId, CancellationToken ct = default)
    {
        var semana = await _semanaListaRepository.GetByIdAsync(semanaId, ct)
            ?? throw new DomainException("Semana de lista não encontrada.");

        // Total separado por material somando todos os locais da semana
        var saidaPorMaterial = semana.ListasLocais
            .SelectMany(l => l.Itens)
            .GroupBy(i => i.MaterialId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.QuantidadeSeparada ?? 0));

        var materiais = await _materialRepository.GetAllAsync(ct);

        // Todos os materiais do cadastro aparecem, mesmo com saída zero, como na planilha de referência
        return materiais
            .Select(m => new ResumoMaterialDto(
                m.Nome.ToUpperInvariant(),
                m.Unidade.ToString(),
                saidaPorMaterial.TryGetValue(m.Id, out var saida) ? saida : 0))
            .ToList();
    }

    public async Task<(byte[] Conteudo, string NomeArquivo)> GerarXlsxAsync(Guid semanaId, CancellationToken ct = default)
    {
        var semana = await _semanaListaRepository.GetByIdAsync(semanaId, ct)
            ?? throw new DomainException("Semana de lista não encontrada.");

        var linhas = await GerarResumoAsync(semanaId, ct);

        using var workbook = new XLWorkbook();
        var planilha = workbook.Worksheets.Add("Semana");

        planilha.Cell(1, 1).Value = "MATERIAL";
        planilha.Cell(1, 2).Value = "UNIDADE";
        planilha.Cell(1, 3).Value = "SAÍDA";

        var cabecalho = planilha.Row(1);
        cabecalho.Style.Font.Bold = true;
        cabecalho.Style.Font.FontColor = XLColor.White;
        cabecalho.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E78");

        var linha = 2;
        foreach (var item in linhas)
        {
            planilha.Cell(linha, 1).Value = item.Material;
            planilha.Cell(linha, 2).Value = item.Unidade;
            planilha.Cell(linha, 3).Value = item.Saida;
            linha++;
        }

        planilha.SheetView.FreezeRows(1);
        planilha.Columns().AdjustToContents();

        // Segunda aba: quem retirou o material de cada local, com data e hora (horário de Brasília)
        var abaRetiradas = workbook.Worksheets.Add("Retiradas");
        var linhasRetirada = semana.ListasLocais
            .OrderBy(l => l.Local?.OrdemExibicao ?? int.MaxValue)
            .Select(l => new object[]
            {
                l.Local?.Nome ?? "Local",
                l.SemPedido ? "Sem pedido" : l.NomeRetiradoPor ?? "Não retirado",
                l.DataRetirada.HasValue ? LMaterial.Application.Services.Relatorios.HorarioBrasilia.Formatar(l.DataRetirada.Value) : "",
                l.SemPedido ? "" : l.Itens.Sum(i => i.QuantidadeSeparada ?? 0)
            });
        PlanilhaHelper.EscreverTabela(abaRetiradas, 1, new[] { "LOCAL", "RESPONSÁVEL PELA RETIRADA", "DATA/HORA", "QTD. ENTREGUE" }, linhasRetirada);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var nomeArquivo = $"semana-lista-{semana.DataInicio:yyyy-MM-dd}-a-{semana.DataFim:yyyy-MM-dd}.xlsx";
        return (stream.ToArray(), nomeArquivo);
    }

    // Lista de um local: pedido, quantidade separada, falta e dados da retirada
    public async Task<(byte[] Conteudo, string NomeArquivo)> GerarXlsxLocalAsync(Guid semanaId, Guid listaLocalId, CancellationToken ct = default)
    {
        var semana = await _semanaListaRepository.GetByIdAsync(semanaId, ct)
            ?? throw new DomainException("Semana de lista não encontrada.");

        var lista = semana.ListasLocais.FirstOrDefault(l => l.Id == listaLocalId)
            ?? throw new DomainException("Lista local não encontrada nesta semana.");

        var materiais = (await _materialRepository.GetAllAsync(ct)).ToDictionary(m => m.Id);
        var nomeLocal = lista.Local?.Nome ?? "Local";

        using var workbook = new XLWorkbook();
        var planilha = workbook.Worksheets.Add("Lista");

        planilha.Cell(1, 1).Value = $"{nomeLocal} - semana {semana.DataInicio:dd/MM/yyyy} a {semana.DataFim:dd/MM/yyyy}";
        planilha.Cell(1, 1).Style.Font.Bold = true;
        planilha.Cell(2, 1).Value = lista.SemPedido
            ? "Sem pedido nesta semana"
            : lista.DataRetirada.HasValue
                ? $"Responsável pela retirada: {lista.NomeRetiradoPor} em {LMaterial.Application.Services.Relatorios.HorarioBrasilia.Formatar(lista.DataRetirada.Value)}"
                : $"Situação: {lista.Status}";

        var linhas = lista.Itens
            .Select(i => new { Item = i, Material = materiais.TryGetValue(i.MaterialId, out var m) ? m : null })
            .OrderBy(x => x.Material?.OrdemExibicao ?? int.MaxValue)
            .Select(x => new object[]
            {
                x.Material?.Nome ?? "Material removido",
                x.Material?.Unidade.ToString() ?? "",
                x.Item.QuantidadeSolicitada,
                x.Item.QuantidadeSeparada.HasValue ? x.Item.QuantidadeSeparada.Value : "",
                x.Item.QuantidadeSeparada.HasValue ? Math.Max(0, x.Item.QuantidadeSolicitada - x.Item.QuantidadeSeparada.Value) : "",
                x.Item.Observacao ?? ""
            });

        PlanilhaHelper.EscreverTabela(planilha, 4, new[] { "MATERIAL", "UNIDADE", "SOLICITADO", "SEPARADO", "FALTA", "OBSERVAÇÃO" }, linhas);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var nomeArquivoLocal = new string(nomeLocal.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-').ToArray());
        return (stream.ToArray(), $"lista-{nomeArquivoLocal}-{semana.DataInicio:yyyy-MM-dd}.xlsx");
    }
}
