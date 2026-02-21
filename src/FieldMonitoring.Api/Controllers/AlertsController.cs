using FieldMonitoring.Application.Alerts;
using Microsoft.AspNetCore.Mvc;

namespace FieldMonitoring.Api.Controllers;

/// <summary>
/// Endpoints de API para operações de alertas.
/// </summary>
[ApiController]
[Route("monitoring/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly GetAlertByIdQuery _getAlertByIdQuery;

    public AlertsController(GetAlertByIdQuery getAlertByIdQuery)
    {
        _getAlertByIdQuery = getAlertByIdQuery;
    }

    /// <summary>
    /// Obtém um alerta pelo seu identificador.
    /// </summary>
    /// <param name="alertId">Identificador único do alerta.</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>Dados do alerta ou 404 se não encontrado.</returns>
    [HttpGet("{alertId:guid}")]
    [ProducesResponseType(typeof(AlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertDto>> GetById(
        Guid alertId,
        CancellationToken cancellationToken)
    {
        AlertDto? dto = await _getAlertByIdQuery.ExecuteAsync(alertId, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }
}
