using FieldMonitoring.Application.Telemetry;
using Microsoft.AspNetCore.Mvc;

namespace FieldMonitoring.Api.Controllers;

/// <summary>
/// Controlador exclusivo para simulação e debug.
/// Permite injetar telemetria via HTTP para facilitar testes manuais sem depender do Worker/SQS.
/// </summary>
[ApiController]
[Route("monitoring/[controller]")]
public class SimulationController : ControllerBase
{
    private readonly ProcessTelemetryReadingUseCase _useCase;
    private readonly InsertMockReadingUseCase _mockUseCase;

    public SimulationController(
        ProcessTelemetryReadingUseCase useCase,
        InsertMockReadingUseCase mockUseCase)
    {
        _useCase = useCase;
        _mockUseCase = mockUseCase;
    }

    /// <summary>
    /// Simula a chegada de uma leitura de telemetria.
    /// Executa exatamente o mesmo caso de uso que o Worker.
    /// </summary>
    /// <param name="message">Mensagem de telemetria simulada.</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>Resultado do processamento da leitura.</returns>
    [HttpPost("telemetry")]
    [ProducesResponseType(typeof(ProcessingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProcessingResult), StatusCodes.Status400BadRequest)]
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

    /// <summary>
    /// Insere uma leitura simulada no InfluxDB para testar conexão.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>Resultado da inserção de leitura simulada.</returns>
    [HttpGet("influx-test")]
    [ProducesResponseType(typeof(ProcessingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProcessingResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TestInfluxConnection(CancellationToken cancellationToken)
    {
        ProcessingResult result = await _mockUseCase.ExecuteAsync(cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
