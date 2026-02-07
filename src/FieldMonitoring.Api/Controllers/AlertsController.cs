using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Domain.Alerts;
using Microsoft.AspNetCore.Mvc;

namespace FieldMonitoring.Api.Controllers;

/// <summary>
/// Endpoints de API para operações de alertas.
/// </summary>
[ApiController]
[Route("monitoring/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly IAlertStore _alertStore;

    public AlertsController(IAlertStore alertStore)
    {
        _alertStore = alertStore;
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
        Alert? alert = await _alertStore.GetByIdAsync(alertId, cancellationToken);
        if (alert == null)
        {
            return NotFound();
        }
        return Ok(AlertDto.FromEntity(alert));
    }

}
