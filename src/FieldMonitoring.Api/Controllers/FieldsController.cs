// ModelBinding removed: use native DateTimeOffset binder for query params
using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Application.Fields;
using FieldMonitoring.Application.Telemetry;
using Microsoft.AspNetCore.Mvc;

namespace FieldMonitoring.Api.Controllers;

/// <summary>
/// Endpoints de API para operações a nível de talhão.
/// </summary>
[ApiController]
[Route("monitoring/[controller]")]
public class FieldsController : ControllerBase
{
    private readonly GetFieldDetailQuery _fieldDetailQuery;
    private readonly GetFieldHistoryQuery _fieldHistoryQuery;
    private readonly GetActiveAlertsQuery _activeAlertsQuery;
    private readonly GetAlertHistoryQuery _alertHistoryQuery;

    public FieldsController(
        GetFieldDetailQuery fieldDetailQuery,
        GetFieldHistoryQuery fieldHistoryQuery,
        GetActiveAlertsQuery activeAlertsQuery,
        GetAlertHistoryQuery alertHistoryQuery)
    {
        _fieldDetailQuery = fieldDetailQuery;
        _fieldHistoryQuery = fieldHistoryQuery;
        _activeAlertsQuery = activeAlertsQuery;
        _alertHistoryQuery = alertHistoryQuery;
    }

    /// <summary>
    /// Obtém informações detalhadas sobre um talhão.
    /// </summary>
    /// <param name="fieldId">Identificador do talhão.</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>Dados detalhados do talhão ou 404 se não encontrado.</returns>
    [HttpGet("{fieldId}")]
    [ProducesResponseType(typeof(FieldDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FieldDetailDto>> GetDetail(
        string fieldId,
        CancellationToken cancellationToken)
    {
        FieldDetailDto? result = await _fieldDetailQuery.ExecuteAsync(fieldId, cancellationToken);
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }

    /// <summary>
    /// Obtém histórico de leituras de um talhão.
    /// </summary>
    /// <param name="fieldId">Identificador do talhão.</param>
    /// <param name="from">Início do período em ISO 8601 com offset (opcional).</param>
    /// <param name="to">Fim do período em ISO 8601 com offset (opcional).</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>Lista de leituras no período informado.</returns>
    [HttpGet("{fieldId}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<ReadingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReadingDto>>> GetHistory(
        string fieldId,
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken cancellationToken = default)
    {
        // Parse query params using a tolerant parser; accept encoded '+' or spaces.
        var parsedTo = FieldMonitoring.Api.Utilities.QueryDateTimeOffsetParser.Parse(to) ?? DateTimeOffset.UtcNow;
        var parsedFrom = FieldMonitoring.Api.Utilities.QueryDateTimeOffsetParser.Parse(from) ?? parsedTo.AddDays(-1);

        IReadOnlyList<ReadingDto> readings = await _fieldHistoryQuery.ExecuteAsync(fieldId, parsedFrom, parsedTo, cancellationToken);
        return Ok(readings);
    }

    /// <summary>
    /// Obtém alertas ativos de um talhão.
    /// </summary>
    /// <param name="fieldId">Identificador do talhão.</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>Lista de alertas ativos do talhão.</returns>
    [HttpGet("{fieldId}/alerts")]
    [ProducesResponseType(typeof(IReadOnlyList<AlertDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertDto>>> GetActiveAlerts(
        string fieldId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AlertDto> result = await _activeAlertsQuery.ExecuteByFieldAsync(fieldId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtém histórico de alertas de um talhão.
    /// </summary>
    /// <param name="fieldId">Identificador do talhão.</param>
    /// <param name="from">Início do período em ISO 8601 com offset (opcional).</param>
    /// <param name="to">Fim do período em ISO 8601 com offset (opcional).</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>Lista de alertas no período informado.</returns>
    [HttpGet("{fieldId}/alerts/history")]
    [ProducesResponseType(typeof(IReadOnlyList<AlertDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertDto>>> GetAlertHistory(
        string fieldId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AlertDto> result = await _alertHistoryQuery.ExecuteByFieldAsync(fieldId, from, to, cancellationToken);
        return Ok(result);
    }
}
