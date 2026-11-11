using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using LMaterial.Application.DTOs.Consumo;
using LMaterial.Application.Services.Planilhas;
using LMaterial.Domain.Entities;
using LMaterial.Domain.Enums;
using LMaterial.Domain.Exceptions;
using LMaterial.Domain.Repositories;

namespace LMaterial.Application.Services.Consumo;

// Consumo = quantidade separada nas listas com retirada registrada, ou seja, o material que efetivamente saiu do almoxarifado.
// Uma semana entra no período quando a data de início dela está dentro do intervalo pedido.
public class ConsumoService : IConsumoService
{
    public const int MaxSemanas = 52;

    private readonly ISemanaListaRepository _semanaListaRepository;
    private readonly IMaterialRepository _materialRepository;
    private readonly ILocalRepository _localRepository;

    public ConsumoService(ISemanaListaRepository semanaListaRepository, IMaterialRepository materialRepository, ILocalRepository localRepository)
    {
        _semanaListaRepository = semanaListaRepository ?? throw new ArgumentNullException(nameof(semanaListaRepository));
        _materialRepository = materialRepository ?? throw new ArgumentNullException(nameof(materialRepository));
        _localRepository = localRepository ?? throw new ArgumentNullException(nameof(localRepository));
    }

    public async Task<ConsumoPeriodoDto> ObterPorPeriodoAsync(DateOnly dataInicio, DateOnly dataFim, Guid? localId, CancellationToken ct = default)
    {
        if (dataInicio > dataFim)
            throw new DomainException("A data de início não pode ser posterior à data de fim.");

        var semanas = (await _semanaListaRepository.GetAllAsync(ct))
            .Where(s => s.DataInicio >= dataInicio && s.DataInicio <= dataFim)
            .ToList();

        var retiradas = semanas
            .SelectMany(s => s.ListasLocais)
            .Where(l => l.Status == StatusListaLocal.Retirado && (!localId.HasValue || l.LocalId == localId.Value))
            .ToList();

        var materiais = (await _materialRepository.GetAllAsync(ct)).ToDictionary(m => m.Id);
        var locais = (await _localRepository.GetAllAsync(ct)).ToDictionary(l => l.Id);

        var porMaterial = retiradas
            .SelectMany(l => l.Itens)
            .GroupBy(i => i.MaterialId)
            .Select(g =>
            {
                materiais.TryGetValue(g.Key, out var m);
                return new ConsumoMaterialDto(g.Key, m?.Nome ?? "Material removido", m?.Unidade.ToString() ?? "", g.Sum(i => i.QuantidadeSeparada ?? 0));
            })
            .Where(x => x.QuantidadeTotal > 0)
            .OrderByDescending(x => x.QuantidadeTotal)
            .ThenBy(x => x.NomeMaterial)
            .ToList();

        var porLocal = retiradas
            .GroupBy(l => l.LocalId)
            .Select(g =>
            {
                locais.TryGetValue(g.Key, out var loc);
                return new
                {
                    Ordem = loc?.OrdemExibicao ?? int.MaxValue,
                    Dto = new ConsumoLocalDto(g.Key, loc?.Nome ?? "Local removido", g.SelectMany(l => l.Itens).Sum(i => i.QuantidadeSeparada ?? 0))
                };
            })
            .Where(x => x.Dto.QuantidadeTotal > 0)
            .OrderBy(x => x.Ordem)
            .Select(x => x.Dto)
            .ToList();

        return new ConsumoPeriodoDto(dataInicio, dataFim, localId, semanas.Count, porMaterial, porLocal);
    }

    // Últimas N semanas já enviadas ao almoxarifado, da mais antiga para a mais recente, com a variação de cada material
    // entre a penúltima e a última semana
    public async Task<ConsumoSemanalDto> ObterEvolucaoSemanalAsync(int quantidadeSemanas, CancellationToken ct = default)
    {
        quantidadeSemanas = Math.Clamp(quantidadeSemanas, 2, MaxSemanas);

        var semanas = (await _semanaListaRepository.GetAllAsync(ct))
            .Where(s => s.Status != StatusSemanaLista.EmMontagem)
            .OrderByDescending(s => s.DataInicio)
            .Take(quantidadeSemanas)
            .OrderBy(s => s.DataInicio)
            .ToList();

        var consumoPorSemana = semanas.Select(ConsumoPorMaterial).ToList();

        var semanasDto = semanas
            .Select((s, idx) => new SemanaConsumoDto(s.Id, s.DataInicio, s.DataFim, s.Status.ToString(), consumoPorSemana[idx].Values.Sum()))
            .ToList();

        var idsComConsumo = consumoPorSemana.SelectMany(c => c.Where(kv => kv.Value > 0).Select(kv => kv.Key)).ToHashSet();
        var materiais = (await _materialRepository.GetAllAsync(ct))
            .Where(m => idsComConsumo.Contains(m.Id))
            .ToList();

        var tendencias = materiais
            .Select(m =>
            {
                var serie = consumoPorSemana.Select(c => c.TryGetValue(m.Id, out var q) ? q : 0).ToList();
                var ultima = serie.Count >= 1 ? serie[^1] : 0;
                var anterior = serie.Count >= 2 ? serie[^2] : 0;
                var variacao = ultima - anterior;
                double? percentual = anterior > 0 ? Math.Round(variacao * 100.0 / anterior, 1) : null;
                return new MaterialTendenciaDto(m.Id, m.Nome, m.Unidade.ToString(), serie, anterior, ultima, variacao, percentual);
            })
            .OrderByDescending(t => t.Variacao)
            .ThenBy(t => t.NomeMaterial)
            .ToList();

        return new ConsumoSemanalDto(semanasDto, tendencias);
    }

    public async Task<(byte[] Conteudo, string NomeArquivo)> ExportarPeriodoXlsxAsync(DateOnly dataInicio, DateOnly dataFim, Guid? localId, CancellationToken ct = default)
    {
        var consumo = await ObterPorPeriodoAsync(dataInicio, dataFim, localId, ct);

        string? nomeLocal = null;
        if (localId.HasValue)
            nomeLocal = (await _localRepository.GetByIdAsync(localId.Value, ct))?.Nome;

        using var workbook = new XLWorkbook();

        var abaMaterial = workbook.Worksheets.Add("Por material");
        abaMaterial.Cell(1, 1).Value = $"Consumo de {dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}" + (nomeLocal != null ? $" - {nomeLocal}" : " - todos os locais");
        abaMaterial.Cell(1, 1).Style.Font.Bold = true;
        PlanilhaHelper.EscreverTabela(abaMaterial, 3, new[] { "MATERIAL", "UNIDADE", "QUANTIDADE" },
            consumo.PorMaterial.Select(m => new object[] { m.NomeMaterial, m.Unidade, m.QuantidadeTotal }));

        if (!localId.HasValue)
        {
            var abaLocal = workbook.Worksheets.Add("Por local");
            PlanilhaHelper.EscreverTabela(abaLocal, 1, new[] { "LOCAL", "QUANTIDADE" },
                consumo.PorLocal.Select(l => new object[] { l.NomeLocal, l.QuantidadeTotal }));
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return (stream.ToArray(), $"consumo-{dataInicio:yyyy-MM-dd}-a-{dataFim:yyyy-MM-dd}.xlsx");
    }

    private static Dictionary<Guid, int> ConsumoPorMaterial(SemanaLista semana)
    {
        return semana.ListasLocais
            .Where(l => l.Status == StatusListaLocal.Retirado)
            .SelectMany(l => l.Itens)
            .GroupBy(i => i.MaterialId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.QuantidadeSeparada ?? 0));
    }
}
