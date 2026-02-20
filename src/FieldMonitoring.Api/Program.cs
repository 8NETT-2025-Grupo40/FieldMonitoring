using FieldMonitoring.Api.ExceptionHandling;
using FieldMonitoring.Api.Extensions;
using FieldMonitoring.Application;
using FieldMonitoring.Application.Serialization;
using FieldMonitoring.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Adiciona serviços da camada Application (use cases, queries, services)
builder.Services.AddApplicationServices();

// Adiciona serviços da camada Infrastructure (adapters, EF Core, etc)
builder.Services.AddInfrastructureServices(builder.Configuration);

// Adiciona verificações de saúde (liveness/readiness)
builder.Services.AddApiHealthChecks(builder.Configuration);

// Adiciona consumidor SQS (mensageria)
builder.Services.AddSqsMessaging(builder.Configuration);

// Adiciona autenticação e autorização com AWS Cognito
builder.Services.AddCognitoUserAuthentication(builder.Configuration);

// Adiciona tratamento global de exceções e ProblemDetails
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Adiciona serviços de API
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new StrictDateTimeOffsetJsonConverter());
    });

builder.Services.AddSwaggerDocumentation();

WebApplication app = builder.Build();

// Configura pipeline HTTP (UseExceptionHandler deve ser o primeiro middleware)
app.UseExceptionHandler();
app.UseSwaggerDocumentation();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapApiHealthEndpoints();

app.Run();

// Torna a classe Program acessível para testes de integração
public partial class Program { }
