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
    [HttpGet("{alertId:guid}")]
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
