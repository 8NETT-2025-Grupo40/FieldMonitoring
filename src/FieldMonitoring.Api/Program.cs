using FieldMonitoring.Api.Extensions;
using FieldMonitoring.Application;
using FieldMonitoring.Application.Serialization;
using FieldMonitoring.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Adiciona serviços da camada Application (use cases, queries, services)
builder.Services.AddApplicationServices();

// Adiciona serviços da camada Infrastructure (adapters, EF Core, etc)
builder.Services.AddInfrastructureServices(builder.Configuration);

// Adiciona consumer SQS (mensageria)
builder.Services.AddSqsMessaging(builder.Configuration);

// Adiciona autenticação e autorização com AWS Cognito
builder.Services.AddCognitoUserAuthentication(builder.Configuration);

// Adiciona serviços de API
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new StrictDateTimeOffsetJsonConverter());
    });

builder.Services.AddSwaggerDocumentation();

WebApplication app = builder.Build();

// Configura pipeline HTTP
app.UseSwaggerDocumentation();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Log de inicialização
ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();

app.Run();

// Torna a classe Program acessível para testes de integração
public partial class Program { }
