using System.Collections.Generic;
using ClosedXML.Excel;

namespace LMaterial.Application.Services.Planilhas;

// Formatação comum das planilhas exportadas: cabeçalho destacado, linha congelada e colunas ajustadas
public static class PlanilhaHelper
{
    public static void EscreverTabela(IXLWorksheet planilha, int linhaCabecalho, IReadOnlyList<string> cabecalhos, IEnumerable<object[]> linhas)
    {
        for (var c = 0; c < cabecalhos.Count; c++)
            planilha.Cell(linhaCabecalho, c + 1).Value = cabecalhos[c];

        var cabecalho = planilha.Range(linhaCabecalho, 1, linhaCabecalho, cabecalhos.Count);
        cabecalho.Style.Font.Bold = true;
        cabecalho.Style.Font.FontColor = XLColor.White;
        cabecalho.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F6D2E");

        var linha = linhaCabecalho + 1;
        foreach (var valores in linhas)
        {
            for (var c = 0; c < valores.Length; c++)
                planilha.Cell(linha, c + 1).Value = XLCellValue.FromObject(valores[c]);
            linha++;
        }

        planilha.SheetView.FreezeRows(linhaCabecalho);
        planilha.Columns().AdjustToContents();
    }
}
