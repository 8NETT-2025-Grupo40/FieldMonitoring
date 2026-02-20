using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FieldMonitoring.Api.ExceptionHandling;

/// <summary>
/// Manipulador global de exceções que converte exceções não tratadas em respostas ProblemDetails.
/// </summary>
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    /// <summary>
    /// HTTP 499 — convenção (nginx) para requisição cancelada pelo cliente.
    /// Não existe em <see cref="StatusCodes"/>, por isso definimos aqui.
    /// </summary>
    private const int StatusClientClosedRequest = 499;

    private readonly IHostEnvironment _environment;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IHostEnvironment environment,
        ILogger<GlobalExceptionHandler> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = MapException(exception);

        LogException(exception, statusCode);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = ResolveDetail(exception, statusCode),
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title) MapException(Exception exception) => exception switch
    {
        ArgumentException => (StatusCodes.Status400BadRequest, "Parâmetro inválido"),
        JsonException => (StatusCodes.Status400BadRequest, "JSON inválido"),
        KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
        OperationCanceledException => (StatusClientClosedRequest, "Requisição cancelada"),
        _ => (StatusCodes.Status500InternalServerError, "Erro interno do servidor"),
    };

    private void LogException(Exception exception, int statusCode)
    {
        if (exception is OperationCanceledException)
        {
            _logger.LogInformation("Requisição cancelada pelo cliente.");
        }
        else if (statusCode >= 500)
        {
            _logger.LogError(exception, "Exceção não tratada: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning(exception, "Erro de requisição ({StatusCode}): {Message}", statusCode, exception.Message);
        }
    }

    private string? ResolveDetail(Exception exception, int statusCode)
    {
        if (statusCode < 500)
        {
            return exception.Message;
        }

        return _environment.IsDevelopment()
            ? exception.ToString()
            : null;
    }
}
