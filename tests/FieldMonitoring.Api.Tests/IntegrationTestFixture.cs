using FieldMonitoring.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;


namespace FieldMonitoring.Api.Tests;

/// <summary>
/// Fixture para testes de integração.
/// Configura WebApplicationFactory com banco in-memory e fornece helpers para reset/seed.
/// </summary>
public class IntegrationTestFixture : TestWebApplicationFactory
{
    private readonly string _databaseName = $"FieldMonitoringTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            // Remove o DbContext configurado no Program.cs
            var descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<FieldMonitoringDbContext>))
                .ToList();
            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }


            // Adiciona DbContext in-memory com database name único por fixture
            services.AddDbContext<FieldMonitoringDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });
        });
    }
    /// <summary>
    /// Reseta o banco de dados, removendo todos os dados.
    /// Chame no construtor de cada teste para garantir isolamento.
    /// </summary>
    public void ResetDatabase()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FieldMonitoringDbContext>();
        
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }

    /// <summary>
    /// Obtém uma instância do DbContext para verificações nos testes.
    /// </summary>
    public FieldMonitoringDbContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<FieldMonitoringDbContext>();
    }
}
