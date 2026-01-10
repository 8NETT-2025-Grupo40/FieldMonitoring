using FieldMonitoring.Application.Telemetry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FieldMonitoring.Api.Controllers;

/// <summary>
/// Controlador exclusivo para simulação e debug.
/// Permite injetar telemetria via HTTP para facilitar testes manuais sem depender do Worker/SQS.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public class SimulationController : ControllerBase
{
    private readonly ProcessTelemetryReadingUseCase _useCase;

    public SimulationController(ProcessTelemetryReadingUseCase useCase)
    {
        _useCase = useCase;
    }

    /// <summary>
    /// Simula a chegada de uma leitura de telemetria.
    /// Executa exatamente o mesmo caso de uso que o Worker.
    /// </summary>
    [HttpPost("telemetry")]
    public async Task<IActionResult> SimulateTelemetry(
        [FromBody] TelemetryReceivedMessage message,
        CancellationToken cancellationToken)
    {
        ProcessingResult result = await _useCase.ExecuteAsync(message, cancellationToken);
        
        if (!result.IsSuccess)
        {
             return BadRequest(result);
        }
        
        return Ok(result);
    }
}
