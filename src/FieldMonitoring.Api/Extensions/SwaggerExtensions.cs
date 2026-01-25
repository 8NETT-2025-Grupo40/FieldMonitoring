using Microsoft.OpenApi;

namespace FieldMonitoring.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "FieldMonitoring API",
                Version = "v1"
            });

            // Configuração para autenticação JWT Bearer
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
            });

            c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        });

        return services;
    }

    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        // Swagger habilitado em todos os ambientes para facilitar testes no EKS
        app.UseSwagger(c =>
        {
            c.RouteTemplate = "monitoring/swagger/{documentName}/swagger.json";
        });
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/monitoring/swagger/v1/swagger.json", "FieldMonitoring API v1");
            c.RoutePrefix = "monitoring/swagger";
        });

        return app;
    }
}
