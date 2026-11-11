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

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var nomeArquivo = $"semana-lista-{semana.DataInicio:yyyy-MM-dd}-a-{semana.DataFim:yyyy-MM-dd}.xlsx";
        return (stream.ToArray(), nomeArquivo);
    }
}
