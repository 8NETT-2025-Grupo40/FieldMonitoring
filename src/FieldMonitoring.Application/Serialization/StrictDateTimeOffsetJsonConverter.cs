using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FieldMonitoring.Application.Serialization;


/// <summary>
/// Conversor JSON estrito para DateTimeOffset
/// - Requer que o valor seja uma string ISO 8601 com offset explícito (ex.: "2026-01-17T12:00:00-03:00" ou "...Z").
/// - Lança JsonException para valores inválidos, evitando ambiguidade de fuso em query/JSON.
/// </summary>
public sealed class StrictDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Esperamos uma string; caso contrário é inválido para nosso contrato.
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("O campo 'timestamp' deve ser uma string com offset.");
        }

        string? value = reader.GetString();
        // Validação simples: usa DateTimeOffset.TryParse para aceitar formatos ISO; exigimos que a string
        // contenha 'Z' ou um deslocamento ±HH:mm. Isso substitui o DateTimeOffsetParser removido.
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("O campo 'timestamp' deve incluir offset (ex.: 2026-01-17T12:00:00-03:00).");
        }

        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset parsed))
        {
            throw new JsonException("O campo 'timestamp' deve ser um timestamp ISO 8601 válido com offset.");
        }

        // Confirma presença de offset explícito: termina com 'Z' ou tem ±HH:mm
        var normalized = value.Trim();
        if (!normalized.EndsWith("Z", StringComparison.OrdinalIgnoreCase) && !System.Text.RegularExpressions.Regex.IsMatch(normalized, @"[+-]\d{2}:\d{2}$"))
        {
            throw new JsonException("O campo 'timestamp' deve incluir offset (ex.: 2026-01-17T12:00:00-03:00).");
        }

        return parsed;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        // Serializa sempre no formato "O" (round-trip ISO 8601) para preservar o offset.
        writer.WriteStringValue(value.ToString("O", CultureInfo.InvariantCulture));
    }
}
