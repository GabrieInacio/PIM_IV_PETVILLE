namespace PetVille.API;

/// <summary>Regras do expediente PetVille, em hora civil de São Paulo.</summary>
public static class HorarioAtendimento
{
    private const int Abre = 8 * 60;
    private const int Fecha = 18 * 60;
    private const int Intervalo = 30;

    private static TimeZoneInfo Zona
    {
        get
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            }
        }
    }

    /// <summary>Interpreta datas sem fuso como horário do pet shop; converte datas UTC/locais.</summary>
    public static DateTime ConverterParaSaoPaulo(DateTime data)
    {
        return data.Kind switch
        {
            DateTimeKind.Utc => TimeZoneInfo.ConvertTimeFromUtc(data, Zona),
            DateTimeKind.Local => TimeZoneInfo.ConvertTime(data, Zona),
            _ => data // datetime-local no site e DatePicker no MAUI: hora civil do pet shop.
        };
    }

    public static string? Validar(DateTime inicio, int duracaoMinutos)
    {
        if (inicio.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return "Atendemos apenas de segunda a sexta-feira.";

        var agora = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Zona).DateTime;
        if (inicio <= agora) return "Escolha uma data e horário futuros (horário de Brasília).";

        if (duracaoMinutos <= 0 || duracaoMinutos > 600)
            return "Duração do serviço inválida. Entre em contato com o pet shop.";

        var minutos = inicio.Hour * 60 + inicio.Minute;
        if (minutos < Abre || minutos >= Fecha || minutos % Intervalo != 0
            || inicio.Second != 0 || inicio.Millisecond != 0)
            return "Escolha um horário de segunda a sexta, a partir das 8h, em intervalos de 30 minutos.";

        if (minutos + duracaoMinutos > Fecha)
            return "O serviço precisa terminar até as 18h. Escolha um horário mais cedo.";

        return null;
    }
}
