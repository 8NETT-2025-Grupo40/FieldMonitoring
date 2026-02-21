using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace FieldMonitoring.Api.Extensions;

/// <summary>
/// Extensões para autenticação de usuários via AWS Cognito User Pool.
/// Use para endpoints acessados por usuários logados (login/senha, redes sociais, etc).
/// </summary>
public static class CognitoUserAuthenticationExtensions
{
    /// <summary>
    /// Configura autenticação JWT para tokens de usuários do Cognito.
    /// </summary>
    /// <remarks>
    /// <para><b>Configurações necessárias no appsettings.json:</b></para>
    /// <list type="bullet">
    ///   <item><c>COGNITO_REGION</c> - Região AWS (ex: us-east-2)</item>
    ///   <item><c>COGNITO_USER_POOL_ID</c> - ID do User Pool (ex: us-east-2_abc123)</item>
    ///   <item><c>COGNITO_CLIENT_ID</c> - ID do App Client para login de usuários</item>
    /// </list>
    /// <para><b>Uso nos controllers:</b></para>
    /// <code>
    /// [Authorize] // Requer token de usuário válido
    /// public IActionResult GetProfile() { ... }
    /// </code>
    /// </remarks>
    public static IServiceCollection AddCognitoUserAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Lê configurações do Cognito do appsettings.json
        var region = configuration["COGNITO_REGION"] ?? string.Empty;
        var userPoolId = configuration["COGNITO_USER_POOL_ID"] ?? string.Empty;
        var clientId = configuration["COGNITO_CLIENT_ID"] ?? string.Empty;

        // Authority = URL do Cognito que emite os tokens (issuer)
        // O middleware usa essa URL para buscar as chaves públicas e validar assinaturas
        var authority = $"https://cognito-idp.{region}.amazonaws.com/{userPoolId}";

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authority,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.FromMinutes(2)
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        // Apenas access tokens são válidos para a API
                        var tokenUse = ctx.Principal?.FindFirst("token_use")?.Value;
                        if (!string.Equals(tokenUse, "access", StringComparison.Ordinal))
                        {
                            ctx.Fail("invalid token_use");
                            return Task.CompletedTask;
                        }

                        // Rejeita tokens de outros app clients
                        var clientIdClaim = ctx.Principal?.FindFirst("client_id")?.Value;
                        if (!string.Equals(clientIdClaim, clientId, StringComparison.Ordinal))
                        {
                            ctx.Fail("invalid client_id");
                            return Task.CompletedTask;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        // Todos os endpoints exigem autenticação por padrão
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
