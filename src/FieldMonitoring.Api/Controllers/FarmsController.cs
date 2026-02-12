// ModelBinding removed: using native DateTimeOffset binder for query params
using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Application.Fields;
using Microsoft.AspNetCore.Mvc;

namespace FieldMonitoring.Api.Controllers;

/// <summary>
/// Endpoints de API para operações a nível de fazenda.
/// Fornece visão geral dos talhões e alertas agregados por fazenda.
/// </summary>
[ApiController]
[Route("monitoring/[controller]")]
public class FarmsController : ControllerBase
{
    private readonly GetFarmOverviewQuery _farmOverviewQuery;
    private readonly GetActiveAlertsQuery _activeAlertsQuery;
    private readonly GetAlertHistoryQuery _alertHistoryQuery;

    public FarmsController(
        GetFarmOverviewQuery farmOverviewQuery,
        GetActiveAlertsQuery activeAlertsQuery,
        GetAlertHistoryQuery alertHistoryQuery)
    {
        _farmOverviewQuery = farmOverviewQuery;
        _activeAlertsQuery = activeAlertsQuery;
        _alertHistoryQuery = alertHistoryQuery;
    }

    /// <summary>
    /// Obtém a visão geral de todos os talhões de uma fazenda.
    /// Retorna status atual, últimas leituras e contagem de alertas.
    /// </summary>
    /// <param name="farmId">Identificador da fazenda.</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>Visão geral da fazenda.</returns>
    [HttpGet("{farmId}/overview")]
    [ProducesResponseType(typeof(FarmOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FarmOverviewDto>> GetOverview(
        string farmId,
        CancellationToken cancellationToken)
    {
        FarmOverviewDto result = await _farmOverviewQuery.ExecuteAsync(farmId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtém alertas ativos de uma fazenda.
    /// Retorna apenas alertas com status Active (não resolvidos).
    /// </summary>
    /// <param name="farmId">Identificador da fazenda.</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>Lista de alertas ativos da fazenda.</returns>
    [HttpGet("{farmId}/alerts")]
    [ProducesResponseType(typeof(IReadOnlyList<AlertDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertDto>>> GetActiveAlerts(
        string farmId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AlertDto> result = await _activeAlertsQuery.ExecuteByFarmAsync(farmId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtém histórico de alertas de uma fazenda.
    /// Inclui alertas ativos e resolvidos dentro do período especificado.
    /// </summary>
    /// <param name="farmId">Identificador da fazenda.</param>
    /// <param name="from">Início do período em ISO 8601 com offset (opcional).</param>
    /// <param name="to">Fim do período em ISO 8601 com offset (opcional).</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>Lista de alertas no período informado.</returns>
    [HttpGet("{farmId}/alerts/history")]
    [ProducesResponseType(typeof(IReadOnlyList<AlertDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertDto>>> GetAlertHistory(
        string farmId,
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken cancellationToken)
    {
        var parsedTo = FieldMonitoring.Api.Utilities.QueryDateTimeOffsetParser.Parse(to);
        var parsedFrom = FieldMonitoring.Api.Utilities.QueryDateTimeOffsetParser.Parse(from);

        IReadOnlyList<AlertDto> result = await _alertHistoryQuery.ExecuteByFarmAsync(farmId, parsedFrom, parsedTo, cancellationToken);
        return Ok(result);
    }
}
