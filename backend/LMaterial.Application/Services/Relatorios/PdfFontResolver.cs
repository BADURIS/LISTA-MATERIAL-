using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PdfSharp.Fonts;

namespace LMaterial.Application.Services.Relatorios;

// O PDFsharp não lê as fontes do sistema sozinho: este resolvedor procura uma fonte sem serifa
// nas pastas comuns do Windows e do Linux (Arial no Windows; Liberation Sans ou DejaVu Sans num servidor Linux).
// Todos os relatórios usam a família "Arial"; o arquivo real é escolhido aqui.
public class PdfFontResolver : IFontResolver
{
    public const string Familia = "Arial";

    // Arquivos candidatos para (negrito, itálico), em ordem de preferência
    private static readonly Dictionary<(bool Negrito, bool Italico), string[]> Candidatos = new()
    {
        [(false, false)] = new[] { "arial.ttf", "LiberationSans-Regular.ttf", "DejaVuSans.ttf" },
        [(true, false)] = new[] { "arialbd.ttf", "LiberationSans-Bold.ttf", "DejaVuSans-Bold.ttf" },
        [(false, true)] = new[] { "ariali.ttf", "LiberationSans-Italic.ttf", "DejaVuSans-Oblique.ttf" },
        [(true, true)] = new[] { "arialbi.ttf", "LiberationSans-BoldItalic.ttf", "DejaVuSans-BoldOblique.ttf" },
    };

    private static readonly ConcurrentDictionary<string, byte[]> Cache = new();
    private static readonly Lazy<string[]> Pastas = new(ObterPastas);
    private static readonly object Trava = new();
    private static bool _configurado;

    public static void Configurar()
    {
        lock (Trava)
        {
            if (_configurado) return;
            GlobalFontSettings.FontResolver = new PdfFontResolver();
            _configurado = true;
        }
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
    {
        // Qualquer família pedida cai na fonte sem serifa disponível. Se faltar a variação negrito/itálico,
        // usa a versão mais próxima e o PDFsharp simula o estilo que faltou.
        if (LocalizarArquivo(bold, italic) is { } exato)
            return new FontResolverInfo(exato);
        if (LocalizarArquivo(bold, false) is { } semItalico)
            return new FontResolverInfo(semItalico, false, italic);
        if (LocalizarArquivo(false, italic) is { } semNegrito)
            return new FontResolverInfo(semNegrito, bold, false);

        var normal = LocalizarArquivo(false, false)
            ?? throw new InvalidOperationException(
                "Nenhuma fonte encontrada para gerar o PDF. No Linux instale o pacote fonts-liberation (ou fonts-dejavu-core).");
        return new FontResolverInfo(normal, bold, italic);
    }

    public byte[]? GetFont(string faceName) => Cache.GetOrAdd(faceName, File.ReadAllBytes);

    private static readonly ConcurrentDictionary<(bool, bool), string?> Encontrados = new();

    private static string? LocalizarArquivo(bool negrito, bool italico) =>
        Encontrados.GetOrAdd((negrito, italico), chave => BuscarArquivo(chave.Item1, chave.Item2));

    private static string? BuscarArquivo(bool negrito, bool italico)
    {
        foreach (var nome in Candidatos[(negrito, italico)])
        {
            foreach (var pasta in Pastas.Value)
            {
                var encontrado = Directory.EnumerateFiles(pasta, nome, new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    MatchCasing = MatchCasing.CaseInsensitive,
                    IgnoreInaccessible = true
                }).FirstOrDefault();

                if (encontrado != null) return encontrado;
            }
        }

        return null;
    }

    private static string[] ObterPastas()
    {
        var pastas = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, "Fontes"),
            Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
            "/usr/share/fonts",
            "/usr/local/share/fonts",
            "/Library/Fonts",
            "/System/Library/Fonts"
        };

        return pastas.Where(p => !string.IsNullOrWhiteSpace(p) && Directory.Exists(p)).Distinct().ToArray();
    }
}
