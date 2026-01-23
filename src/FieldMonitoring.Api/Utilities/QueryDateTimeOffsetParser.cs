using System;
using System.Globalization;

namespace FieldMonitoring.Api.Utilities;

public static class QueryDateTimeOffsetParser
{
    // Parseia um valor bruto de query string para DateTimeOffset.
    // Comportamento: trim -> substitui espaços por '+' (corrige casos onde '+' não foi URL-encoded)
    // -> tenta DateTimeOffset.TryParse com InvariantCulture + RoundtripKind.
    // Retorna null se não conseguir parsear.
    public static DateTimeOffset? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var s = raw.Trim();

        // Recupera caso comum onde '+' em offsets vira espaço na query string.
        s = s.Replace(' ', '+');

        if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
        {
            return dt;
        }

        return null;
    }
}
