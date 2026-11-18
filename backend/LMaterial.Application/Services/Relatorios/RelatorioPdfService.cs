using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Application.Services.Consumo;
using LMaterial.Application.Services.Planilhas;
using LMaterial.Domain.Entities;
using LMaterial.Domain.Enums;
using LMaterial.Domain.Exceptions;
using LMaterial.Domain.Repositories;
using MigraDoc.DocumentObjectModel;

namespace LMaterial.Application.Services.Relatorios;

public interface IRelatorioPdfService
{
    Task<(byte[] Conteudo, string NomeArquivo)> GerarListaLocalAsync(Guid semanaId, Guid listaLocalId, CancellationToken ct = default);
    Task<(byte[] Conteudo, string NomeArquivo)> GerarSemanaAsync(Guid semanaId, CancellationToken ct = default);
    Task<(byte[] Conteudo, string NomeArquivo)> GerarConsumoAsync(DateOnly dataInicio, DateOnly dataFim, Guid? localId, CancellationToken ct = default);
}

public class RelatorioPdfService : IRelatorioPdfService
{
    private static readonly Dictionary<StatusListaLocal, string> StatusLista = new()
    {
        [StatusListaLocal.Pendente] = "Pendente",
        [StatusListaLocal.Separado] = "Separado",
        [StatusListaLocal.ConfirmadoComPendencia] = "Recebido com pendência",
        [StatusListaLocal.Confirmado] = "Recebido",
        [StatusListaLocal.Retirado] = "Retirado",
    };

    private static readonly Dictionary<StatusSemanaLista, string> StatusSemana = new()
    {
        [StatusSemanaLista.EmMontagem] = "Em montagem",
        [StatusSemanaLista.EnviadaParaSeparacao] = "Em separação",
        [StatusSemanaLista.SeparacaoConcluida] = "Separação concluída",
        [StatusSemanaLista.Finalizada] = "Finalizada",
    };

    private readonly ISemanaListaRepository _semanaListaRepository;
    private readonly IMaterialRepository _materialRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ILocalRepository _localRepository;
    private readonly IPlanilhaSemanaService _planilhaSemanaService;
    private readonly IConsumoService _consumoService;

    public RelatorioPdfService(
        ISemanaListaRepository semanaListaRepository,
        IMaterialRepository materialRepository,
        IUsuarioRepository usuarioRepository,
        ILocalRepository localRepository,
        IPlanilhaSemanaService planilhaSemanaService,
        IConsumoService consumoService)
    {
        _semanaListaRepository = semanaListaRepository ?? throw new ArgumentNullException(nameof(semanaListaRepository));
        _materialRepository = materialRepository ?? throw new ArgumentNullException(nameof(materialRepository));
        _usuarioRepository = usuarioRepository ?? throw new ArgumentNullException(nameof(usuarioRepository));
        _localRepository = localRepository ?? throw new ArgumentNullException(nameof(localRepository));
        _planilhaSemanaService = planilhaSemanaService ?? throw new ArgumentNullException(nameof(planilhaSemanaService));
        _consumoService = consumoService ?? throw new ArgumentNullException(nameof(consumoService));
    }

    // Lista de um local: pedido, separação, faltas e comprovante da retirada (com espaço para assinatura em papel)
    public async Task<(byte[] Conteudo, string NomeArquivo)> GerarListaLocalAsync(Guid semanaId, Guid listaLocalId, CancellationToken ct = default)
    {
        var semana = await ObterSemanaAsync(semanaId, ct);
        var lista = semana.ListasLocais.FirstOrDefault(l => l.Id == listaLocalId)
            ?? throw new DomainException("Lista local não encontrada nesta semana.");

        var materiais = (await _materialRepository.GetAllAsync(ct)).ToDictionary(m => m.Id);
        var nomeLocal = lista.Local?.Nome ?? (await _localRepository.GetByIdAsync(lista.LocalId, ct))?.Nome ?? "Local";

        var pdf = new PdfDocumento($"Lista de Material - {nomeLocal}", $"Semana de {Periodo(semana)}");
        pdf.Campo("Situação", lista.SemPedido ? "Sem pedido nesta semana" : StatusLista[lista.Status]);
        if (lista.DataSeparacao.HasValue)
            pdf.Campo("Separado em", HorarioBrasilia.Formatar(lista.DataSeparacao.Value));

        if (lista.SemPedido)
        {
            pdf.Aviso("Este local foi marcado como sem pedido nesta semana: não há materiais a separar ou retirar.");
            return (pdf.Gerar(), NomeArquivo("lista", nomeLocal, semana));
        }

        var itens = lista.Itens
            .Select(i => new { Item = i, Material = materiais.TryGetValue(i.MaterialId, out var m) ? m : null })
            .OrderBy(x => x.Material?.OrdemExibicao ?? int.MaxValue)
            .ToList();

        var comFalta = new HashSet<int>();
        var linhas = new List<string[]>();
        for (var idx = 0; idx < itens.Count; idx++)
        {
            var (item, material) = (itens[idx].Item, itens[idx].Material);
            var falta = item.QuantidadeSeparada.HasValue ? Math.Max(0, item.QuantidadeSolicitada - item.QuantidadeSeparada.Value) : (int?)null;
            if (falta > 0) comFalta.Add(idx);

            linhas.Add(new[]
            {
                material?.Nome ?? "Material removido",
                material?.Unidade.ToString() ?? "",
                item.QuantidadeSolicitada.ToString(),
                item.QuantidadeSeparada?.ToString() ?? "-",
                falta switch { null => "-", 0 => "OK", _ => falta.Value.ToString() },
                item.Observacao ?? ""
            });
        }

        pdf.Titulo($"Materiais ({itens.Count})");
        pdf.Tabela(
            new[]
            {
                new ColunaPdf("Material", 6.2),
                new ColunaPdf("Unid.", 1.3, ParagraphAlignment.Center),
                new ColunaPdf("Pedido", 1.6, ParagraphAlignment.Right),
                new ColunaPdf("Separado", 1.8, ParagraphAlignment.Right),
                new ColunaPdf("Falta", 1.4, ParagraphAlignment.Right),
                new ColunaPdf("Observação", 5.7),
            },
            linhas,
            comFalta,
            new[]
            {
                "Total",
                "",
                itens.Sum(x => x.Item.QuantidadeSolicitada).ToString(),
                itens.Sum(x => x.Item.QuantidadeSeparada ?? 0).ToString(),
                itens.Sum(x => x.Item.QuantidadeSeparada.HasValue ? Math.Max(0, x.Item.QuantidadeSolicitada - x.Item.QuantidadeSeparada.Value) : 0).ToString(),
                ""
            });

        pdf.Titulo("Retirada");
        if (lista.Status == StatusListaLocal.Retirado && lista.DataRetirada.HasValue)
        {
            var registradoPor = lista.RegistradoPorUsuarioId.HasValue
                ? (await _usuarioRepository.GetByIdAsync(lista.RegistradoPorUsuarioId.Value, ct))?.NomeCompleto
                : null;

            pdf.Campo("Responsável pela retirada", lista.NomeRetiradoPor ?? "");
            pdf.Campo("Data e hora", $"{HorarioBrasilia.Formatar(lista.DataRetirada.Value)} (horário de Brasília)");
            if (registradoPor != null)
                pdf.Campo("Registrado no sistema por", registradoPor);

            var valida = lista.VerificarAssinaturaDigital();
            var assinatura = pdf.Texto("");
            assinatura.AddFormattedText("Assinatura digital: ", TextFormat.Bold);
            var situacao = assinatura.AddFormattedText(valida ? "VÁLIDA" : "INVÁLIDA (dados alterados após a retirada)", TextFormat.Bold);
            situacao.Color = valida ? PdfDocumento.Verde : PdfDocumento.Vermelho;
            var codigo = pdf.Texto(lista.AssinaturaDigital ?? "");
            codigo.Format.Font.Size = 7;
            codigo.Format.Font.Color = PdfDocumento.Cinza;
        }
        else
        {
            pdf.Texto("Retirada ainda não registrada no sistema. Use os campos abaixo para o registro em papel.");
            pdf.Campo("Responsável pela retirada", "______________________________________________");
            pdf.Campo("Data e hora", "____/____/________   ____:____");
        }

        pdf.LinhasAssinatura("Assinatura do responsável pela retirada", "Assinatura do almoxarife");

        return (pdf.Gerar(), NomeArquivo("lista", nomeLocal, semana));
    }

    // Consolidado da semana: total separado por material e situação de cada local
    public async Task<(byte[] Conteudo, string NomeArquivo)> GerarSemanaAsync(Guid semanaId, CancellationToken ct = default)
    {
        var semana = await ObterSemanaAsync(semanaId, ct);
        var resumo = await _planilhaSemanaService.GerarResumoAsync(semanaId, ct);
        var locais = (await _localRepository.GetAllAsync(ct)).ToDictionary(l => l.Id);

        var pdf = new PdfDocumento("Levantamento da Semana", $"Semana de {Periodo(semana)} - {StatusSemana[semana.Status]}");

        var comSaida = resumo.Where(r => r.Saida > 0).ToList();
        pdf.Titulo("Saída por material");
        pdf.Texto($"Total separado somando todos os locais. Materiais sem saída na semana não aparecem ({resumo.Count - comSaida.Count} omitidos).")
            .Format.Font.Color = PdfDocumento.Cinza;
        pdf.Tabela(
            new[]
            {
                new ColunaPdf("Material", 12),
                new ColunaPdf("Unidade", 2.5, ParagraphAlignment.Center),
                new ColunaPdf("Saída", 3.5, ParagraphAlignment.Right),
            },
            comSaida.Select(r => new[] { r.Material, r.Unidade, r.Saida.ToString() }),
            total: new[] { "Total", "", comSaida.Sum(r => r.Saida).ToString() });

        var listas = semana.ListasLocais
            .Select(l => new { Lista = l, Local = locais.TryGetValue(l.LocalId, out var loc) ? loc : l.Local })
            .OrderBy(x => x.Local?.OrdemExibicao ?? int.MaxValue)
            .ToList();

        var pendentes = new HashSet<int>();
        var linhas = new List<string[]>();
        for (var idx = 0; idx < listas.Count; idx++)
        {
            var l = listas[idx].Lista;
            if (l.Status == StatusListaLocal.ConfirmadoComPendencia || l.Itens.Any(i => i.StatusItem == StatusItem.Faltando))
                pendentes.Add(idx);

            linhas.Add(new[]
            {
                listas[idx].Local?.Nome ?? "Local",
                l.SemPedido ? "Sem pedido" : StatusLista[l.Status],
                l.SemPedido ? "-" : l.Itens.Count.ToString(),
                l.SemPedido ? "-" : l.Itens.Sum(i => i.QuantidadeSeparada ?? 0).ToString(),
                l.NomeRetiradoPor ?? "",
                l.DataRetirada.HasValue ? HorarioBrasilia.Formatar(l.DataRetirada.Value) : ""
            });
        }

        pdf.Titulo("Situação por local");
        pdf.Tabela(
            new[]
            {
                new ColunaPdf("Local", 3.4),
                new ColunaPdf("Situação", 3.3),
                new ColunaPdf("Itens", 1.3, ParagraphAlignment.Right),
                new ColunaPdf("Separado", 1.8, ParagraphAlignment.Right),
                new ColunaPdf("Responsável retirada", 5),
                new ColunaPdf("Data", 3.2),
            },
            linhas,
            pendentes);

        if (pendentes.Count > 0)
            pdf.Texto("Linhas em vermelho: locais com itens entregues em quantidade menor que a pedida.").Format.Font.Color = PdfDocumento.Cinza;

        return (pdf.Gerar(), NomeArquivo("levantamento", null, semana));
    }

    // Consumo (material que saiu com retirada registrada) num período, geral ou de um local
    public async Task<(byte[] Conteudo, string NomeArquivo)> GerarConsumoAsync(DateOnly dataInicio, DateOnly dataFim, Guid? localId, CancellationToken ct = default)
    {
        var consumo = await _consumoService.ObterPorPeriodoAsync(dataInicio, dataFim, localId, ct);
        string? nomeLocal = null;
        if (localId.HasValue)
            nomeLocal = (await _localRepository.GetByIdAsync(localId.Value, ct))?.Nome ?? "Local";

        var pdf = new PdfDocumento(
            "Relatório de Consumo",
            $"{dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy} - {nomeLocal ?? "todos os locais"}");
        pdf.Texto($"{consumo.QuantidadeSemanas} semana(s) no período. Consumo = quantidade separada nos locais com retirada registrada.")
            .Format.Font.Color = PdfDocumento.Cinza;

        var totalGeral = consumo.PorMaterial.Sum(m => m.QuantidadeTotal);
        pdf.Titulo("Por material");
        pdf.Tabela(
            new[]
            {
                new ColunaPdf("Material", 10),
                new ColunaPdf("Unidade", 2.5, ParagraphAlignment.Center),
                new ColunaPdf("Quantidade", 3, ParagraphAlignment.Right),
                new ColunaPdf("% do total", 2.5, ParagraphAlignment.Right),
            },
            consumo.PorMaterial.Select(m => new[]
            {
                m.NomeMaterial,
                m.Unidade,
                m.QuantidadeTotal.ToString(),
                totalGeral > 0 ? $"{m.QuantidadeTotal * 100.0 / totalGeral:0.0}%" : "-"
            }),
            total: new[] { "Total", "", totalGeral.ToString(), totalGeral > 0 ? "100%" : "-" });

        if (!localId.HasValue)
        {
            pdf.Titulo("Por local");
            pdf.Tabela(
                new[]
                {
                    new ColunaPdf("Local", 12.5),
                    new ColunaPdf("Quantidade", 3, ParagraphAlignment.Right),
                    new ColunaPdf("% do total", 2.5, ParagraphAlignment.Right),
                },
                consumo.PorLocal.Select(l => new[]
                {
                    l.NomeLocal,
                    l.QuantidadeTotal.ToString(),
                    totalGeral > 0 ? $"{l.QuantidadeTotal * 100.0 / totalGeral:0.0}%" : "-"
                }),
                total: new[] { "Total", consumo.PorLocal.Sum(l => l.QuantidadeTotal).ToString(), totalGeral > 0 ? "100%" : "-" });
        }

        var sufixo = nomeLocal != null ? $"-{Slug(nomeLocal)}" : "";
        return (pdf.Gerar(), $"consumo-{dataInicio:yyyy-MM-dd}-a-{dataFim:yyyy-MM-dd}{sufixo}.pdf");
    }

    private async Task<SemanaLista> ObterSemanaAsync(Guid semanaId, CancellationToken ct)
    {
        return await _semanaListaRepository.GetByIdAsync(semanaId, ct)
            ?? throw new DomainException("Semana de lista não encontrada.");
    }

    private static string Periodo(SemanaLista semana) => $"{semana.DataInicio:dd/MM/yyyy} a {semana.DataFim:dd/MM/yyyy}";

    private static string NomeArquivo(string tipo, string? nomeLocal, SemanaLista semana) =>
        $"{tipo}{(nomeLocal != null ? "-" + Slug(nomeLocal) : "")}-{semana.DataInicio:yyyy-MM-dd}.pdf";

    // Nome de arquivo sem acentos nem espaços
    private static string Slug(string texto)
    {
        var semAcento = new string(texto.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray());
        return System.Text.RegularExpressions.Regex.Replace(semAcento.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
    }
}
