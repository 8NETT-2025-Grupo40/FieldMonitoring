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
        var region = configuration["COGNITO_REGION"];
        var userPoolId = configuration["COGNITO_USER_POOL_ID"];
        var clientId = configuration["COGNITO_CLIENT_ID"];

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

                // Validações customizadas após o token ser decodificado e assinatura verificada
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        // Cognito emite 2 tipos de token: "access" e "id"
                        // Access token = autorização para acessar recursos
                        // ID token = informações do usuário (nome, email, etc)
                        // Para APIs, sempre usamos access token
                        var tokenUse = ctx.Principal?.FindFirst("token_use")?.Value;
                        if (!string.Equals(tokenUse, "access", StringComparison.Ordinal))
                        {
                            ctx.Fail("invalid token_use");
                            return Task.CompletedTask;
                        }

                        // Valida que o token foi emitido pelo App Client correto
                        // Evita que tokens de outros apps do mesmo User Pool sejam aceitos
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

        // FallbackPolicy = política aplicada a TODOS os endpoints que não têm [Authorize] explícito
        // RequireAuthenticatedUser = exige token válido em todas as rotas por padrão
        // Para liberar um endpoint, use [AllowAnonymous]
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
