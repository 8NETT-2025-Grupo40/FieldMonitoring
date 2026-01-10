using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Application.Fields;
using FieldMonitoring.Application.Telemetry;
using FieldMonitoring.Domain.Telemetry;
using Microsoft.AspNetCore.Mvc;

namespace FieldMonitoring.Api.Controllers;

/// <summary>
/// Endpoints de API para operações a nível de talhão.
/// </summary>
[ApiController]
[Route("api/[controller]")]
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
    [HttpGet("{fieldId}")]
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
    /// <param name="aggregation">Intervalo de agregação: "none", "hour", ou "day".</param>
    /// <returns>Lista de leituras ou leituras agregadas.</returns>
    [HttpGet("{fieldId}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<ReadingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IReadOnlyList<ReadingAggregationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        string fieldId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string aggregation = "none",
        CancellationToken cancellationToken = default)
    {
        AggregationInterval interval = aggregation.ToLowerInvariant() switch
        {
            "hour" => AggregationInterval.Hour,
            "day" => AggregationInterval.Day,
            _ => AggregationInterval.None
        };

        if (interval == AggregationInterval.None)
        {
            IReadOnlyList<ReadingDto> readings = await _fieldHistoryQuery.ExecuteAsync(fieldId, from, to, cancellationToken);
            return Ok(readings);
        }
        else
        {
            IReadOnlyList<ReadingAggregationDto> aggregations = await _fieldHistoryQuery.ExecuteAggregatedAsync(fieldId, from, to, interval, cancellationToken);
            return Ok(aggregations);
        }
    }

    /// <summary>
    /// Obtém alertas ativos de um talhão.
    /// </summary>
    [HttpGet("{fieldId}/alerts")]
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
    [HttpGet("{fieldId}/alerts/history")]
    [ProducesResponseType(typeof(IReadOnlyList<AlertDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertDto>>> GetAlertHistory(
        string fieldId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AlertDto> result = await _alertHistoryQuery.ExecuteByFieldAsync(fieldId, from, to, cancellationToken);
        return Ok(result);
    }
}
