using FieldMonitoring.Api.ExceptionHandling;
using FieldMonitoring.Api.Extensions;
using FieldMonitoring.Application;
using FieldMonitoring.Application.Serialization;
using FieldMonitoring.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApiHealthChecks(builder.Configuration);
builder.Services.AddSqsMessaging(builder.Configuration);
builder.Services.AddCognitoUserAuthentication(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new StrictDateTimeOffsetJsonConverter());
    });

builder.Services.AddSwaggerDocumentation();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseSwaggerDocumentation();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapApiHealthEndpoints();

app.Run();

public partial class Program { }
