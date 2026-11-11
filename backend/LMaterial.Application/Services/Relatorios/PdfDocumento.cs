using System.Collections.Generic;
using System.IO;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

namespace LMaterial.Application.Services.Relatorios;

public record ColunaPdf(string Titulo, double LarguraCm, ParagraphAlignment Alinhamento = ParagraphAlignment.Left);

// Montagem padronizada dos relatórios em PDF: cabeçalho Normatel, tabelas com cabeçalho repetido a cada página
// e rodapé com data de geração e numeração "Página X de Y". Página A4 retrato, área útil de 18 cm.
public class PdfDocumento
{
    public static readonly Color Verde = new(31, 109, 46);
    public static readonly Color VerdeClaro = new(234, 243, 222);
    public static readonly Color Cinza = new(100, 116, 139);
    public static readonly Color CinzaClaro = new(241, 245, 249);
    public static readonly Color VermelhoClaro = new(253, 226, 226);
    public static readonly Color Vermelho = new(153, 27, 27);

    private readonly Document _documento;

    public Section Secao { get; }

    public PdfDocumento(string titulo, string subtitulo)
    {
        PdfFontResolver.Configurar();

        _documento = new Document();
        _documento.Info.Title = titulo;
        _documento.Info.Author = "Normatel Engenharia";

        var normal = _documento.Styles[StyleNames.Normal]!;
        normal.Font.Name = PdfFontResolver.Familia;
        normal.Font.Size = 9;

        Secao = _documento.AddSection();
        Secao.PageSetup = _documento.DefaultPageSetup.Clone();
        Secao.PageSetup.PageFormat = PageFormat.A4;
        Secao.PageSetup.TopMargin = Unit.FromCentimeter(1.5);
        Secao.PageSetup.BottomMargin = Unit.FromCentimeter(1.8);
        Secao.PageSetup.LeftMargin = Unit.FromCentimeter(1.5);
        Secao.PageSetup.RightMargin = Unit.FromCentimeter(1.5);
        Secao.PageSetup.FooterDistance = Unit.FromCentimeter(0.8);

        var marca = Secao.AddParagraph("NORMATEL ENGENHARIA");
        marca.Format.Font.Size = 8;
        marca.Format.Font.Bold = true;
        marca.Format.Font.Color = Verde;

        var paragrafoTitulo = Secao.AddParagraph(titulo);
        paragrafoTitulo.Format.Font.Size = 15;
        paragrafoTitulo.Format.Font.Bold = true;
        paragrafoTitulo.Format.SpaceBefore = Unit.FromPoint(2);

        var paragrafoSubtitulo = Secao.AddParagraph(subtitulo);
        paragrafoSubtitulo.Format.Font.Size = 10;
        paragrafoSubtitulo.Format.Font.Color = Cinza;
        paragrafoSubtitulo.Format.SpaceAfter = Unit.FromPoint(4);
        paragrafoSubtitulo.Format.Borders.Bottom.Width = 1;
        paragrafoSubtitulo.Format.Borders.Bottom.Color = Verde;
        paragrafoSubtitulo.Format.Borders.DistanceFromBottom = Unit.FromPoint(6);

        var rodape = Secao.Footers.Primary.AddParagraph();
        rodape.Format.Font.Size = 7.5;
        rodape.Format.Font.Color = Cinza;
        rodape.Format.AddTabStop(Unit.FromCentimeter(18), TabAlignment.Right);
        rodape.AddText($"Gerado em {HorarioBrasilia.Agora()} (horário de Brasília)");
        rodape.AddTab();
        rodape.AddText("Página ");
        rodape.AddPageField();
        rodape.AddText(" de ");
        rodape.AddNumPagesField();
    }

    public Paragraph Titulo(string texto)
    {
        var p = Secao.AddParagraph(texto);
        p.Format.Font.Size = 11;
        p.Format.Font.Bold = true;
        p.Format.SpaceBefore = Unit.FromPoint(12);
        p.Format.SpaceAfter = Unit.FromPoint(4);
        p.Format.KeepWithNext = true;
        return p;
    }

    public Paragraph Texto(string texto, bool negrito = false)
    {
        var p = Secao.AddParagraph(texto);
        p.Format.Font.Bold = negrito;
        p.Format.SpaceAfter = Unit.FromPoint(2);
        return p;
    }

    // Linha "Rótulo: valor"
    public void Campo(string rotulo, string valor)
    {
        var p = Secao.AddParagraph();
        p.Format.SpaceAfter = Unit.FromPoint(2);
        p.AddFormattedText($"{rotulo}: ", TextFormat.Bold);
        p.AddText(valor);
    }

    public void Aviso(string texto)
    {
        var p = Secao.AddParagraph(texto);
        p.Format.Shading.Color = CinzaClaro;
        p.Format.Borders.Distance = Unit.FromPoint(5);
        p.Format.Borders.Width = 0.5;
        p.Format.Borders.Color = new Color(203, 213, 225);
        p.Format.SpaceBefore = Unit.FromPoint(6);
        p.Format.SpaceAfter = Unit.FromPoint(8);
    }

    // Linhas destacadas em vermelho claro (ex.: itens com falta) são indicadas pelo índice
    public Table Tabela(IReadOnlyList<ColunaPdf> colunas, IEnumerable<string[]> linhas, ISet<int>? destacar = null, string[]? total = null)
    {
        var tabela = Secao.AddTable();
        tabela.Borders.Width = 0.4;
        tabela.Borders.Color = new Color(203, 213, 225);
        tabela.TopPadding = Unit.FromPoint(3);
        tabela.BottomPadding = Unit.FromPoint(3);
        tabela.LeftPadding = Unit.FromPoint(4);
        tabela.RightPadding = Unit.FromPoint(4);

        foreach (var coluna in colunas)
            tabela.AddColumn(Unit.FromCentimeter(coluna.LarguraCm));

        var cabecalho = tabela.AddRow();
        cabecalho.HeadingFormat = true;
        cabecalho.Shading.Color = Verde;
        cabecalho.Format.Font.Bold = true;
        cabecalho.Format.Font.Color = Colors.White;
        cabecalho.Format.Font.Size = 8;
        for (var c = 0; c < colunas.Count; c++)
        {
            cabecalho.Cells[c].AddParagraph(colunas[c].Titulo.ToUpperInvariant());
            cabecalho.Cells[c].Format.Alignment = colunas[c].Alinhamento;
        }

        var indice = 0;
        foreach (var valores in linhas)
        {
            var linha = tabela.AddRow();
            if (destacar != null && destacar.Contains(indice))
                linha.Shading.Color = VermelhoClaro;
            else if (indice % 2 == 1)
                linha.Shading.Color = new Color(248, 250, 252);

            for (var c = 0; c < colunas.Count; c++)
            {
                linha.Cells[c].AddParagraph(c < valores.Length ? valores[c] : "");
                linha.Cells[c].Format.Alignment = colunas[c].Alinhamento;
            }
            indice++;
        }

        if (indice == 0)
        {
            var vazia = tabela.AddRow();
            vazia.Cells[0].MergeRight = colunas.Count - 1;
            vazia.Cells[0].AddParagraph("Nenhum registro.");
            vazia.Cells[0].Format.Font.Color = Cinza;
        }

        if (total != null)
        {
            var linhaTotal = tabela.AddRow();
            linhaTotal.Shading.Color = VerdeClaro;
            linhaTotal.Format.Font.Bold = true;
            for (var c = 0; c < colunas.Count; c++)
            {
                linhaTotal.Cells[c].AddParagraph(c < total.Length ? total[c] : "");
                linhaTotal.Cells[c].Format.Alignment = colunas[c].Alinhamento;
            }
        }

        return tabela;
    }

    // Espaço para assinatura em papel
    public void LinhasAssinatura(params string[] rotulos)
    {
        var tabela = Secao.AddTable();
        tabela.Borders.Width = 0;
        var largura = 18.0 / rotulos.Length;
        foreach (var _ in rotulos)
            tabela.AddColumn(Unit.FromCentimeter(largura));

        var linha = tabela.AddRow();
        linha.TopPadding = Unit.FromCentimeter(1.4);
        for (var c = 0; c < rotulos.Length; c++)
        {
            var p = linha.Cells[c].AddParagraph(rotulos[c]);
            p.Format.Alignment = ParagraphAlignment.Center;
            p.Format.Font.Size = 8;
            p.Format.Font.Color = Cinza;
            p.Format.Borders.Top.Width = 0.6;
            p.Format.Borders.Top.Color = Colors.Black;
            p.Format.LeftIndent = Unit.FromCentimeter(0.6);
            p.Format.RightIndent = Unit.FromCentimeter(0.6);
        }
    }

    public byte[] Gerar()
    {
        var renderizador = new PdfDocumentRenderer { Document = _documento };
        renderizador.RenderDocument();

        using var stream = new MemoryStream();
        renderizador.PdfDocument.Save(stream, false);
        return stream.ToArray();
    }
}
