using System;

namespace LMaterial.Application.Services.Relatorios;

// Datas são gravadas em UTC; nos relatórios aparecem no horário de Brasília, seja qual for o fuso do servidor
public static class HorarioBrasilia
{
    private static readonly TimeZoneInfo Fuso = ObterFuso();

    public static DateTime Converter(DateTime data)
    {
        var utc = data.Kind switch
        {
            DateTimeKind.Utc => data,
            DateTimeKind.Local => data.ToUniversalTime(),
            _ => DateTime.SpecifyKind(data, DateTimeKind.Utc)
        };
        return TimeZoneInfo.ConvertTimeFromUtc(utc, Fuso);
    }

    public static string Formatar(DateTime data) => Converter(data).ToString("dd/MM/yyyy HH:mm");

    public static string Agora() => Formatar(DateTime.UtcNow);

    private static TimeZoneInfo ObterFuso()
    {
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
        }

        // Brasília não tem horário de verão desde 2019
        return TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "Brasília", "Brasília");
    }
}
