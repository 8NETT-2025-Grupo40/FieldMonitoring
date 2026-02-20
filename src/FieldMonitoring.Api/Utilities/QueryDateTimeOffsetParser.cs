using System.Globalization;

namespace FieldMonitoring.Api.Utilities;

public static class QueryDateTimeOffsetParser
{
    public const string InvalidFromMessage = "Parâmetro 'from' inválido. Use ISO 8601 com offset.";
    public const string InvalidToMessage = "Parâmetro 'to' inválido. Use ISO 8601 com offset.";
    public const string InvalidRangeMessage = "Parâmetro 'from' deve ser menor ou igual a 'to'.";

    /// <summary>
    /// Tenta converter um valor de query string para DateTimeOffset.
    /// Valores vazios retornam sucesso com valor nulo.
    /// </summary>
    public static bool TryParse(string? raw, out DateTimeOffset? parsed)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            parsed = null;
            return true;
        }

        var s = raw.Trim();

        // Recupera caso comum onde '+' em offsets vira espaço na query string.
        s = s.Replace(' ', '+');

        if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
        {
            parsed = dt;
            return true;
        }

        parsed = null;
        return false;
    }

    /// <summary>
    /// Tenta converter e validar um intervalo opcional (from/to).
    /// </summary>
    public static bool TryParseRange(
        string? fromRaw,
        string? toRaw,
        out DateTimeOffset? from,
        out DateTimeOffset? to,
        out string? validationMessage)
    {
        if (!TryParse(fromRaw, out from))
        {
            to = null;
            validationMessage = InvalidFromMessage;
            return false;
        }

        if (!TryParse(toRaw, out to))
        {
            validationMessage = InvalidToMessage;
            return false;
        }

        if (from.HasValue && to.HasValue && from > to)
        {
            validationMessage = InvalidRangeMessage;
            return false;
        }

        validationMessage = null;
        return true;
    }

    /// <summary>
    /// Tenta resolver um intervalo, aplicando janela padrão quando necessário.
    /// </summary>
    public static bool TryResolveRange(
        string? fromRaw,
        string? toRaw,
        TimeSpan defaultWindow,
        out DateTimeOffset from,
        out DateTimeOffset to,
        out string? validationMessage)
    {
        if (!TryParseRange(fromRaw, toRaw, out DateTimeOffset? parsedFrom, out DateTimeOffset? parsedTo, out validationMessage))
        {
            from = default;
            to = default;
            return false;
        }

        to = parsedTo ?? DateTimeOffset.UtcNow;
        from = parsedFrom ?? to.Subtract(defaultWindow);

        if (from > to)
        {
            validationMessage = InvalidRangeMessage;
            return false;
        }

        validationMessage = null;
        return true;
    }
}
