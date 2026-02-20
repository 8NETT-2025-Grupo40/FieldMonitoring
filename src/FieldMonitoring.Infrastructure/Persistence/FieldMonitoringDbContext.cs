using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Fields;
using FieldMonitoring.Domain.Telemetry;
using Microsoft.EntityFrameworkCore;

namespace FieldMonitoring.Infrastructure.Persistence;

/// <summary>
/// DbContext do Entity Framework Core para FieldMonitoring.
/// </summary>
public class FieldMonitoringDbContext : DbContext
{
    public FieldMonitoringDbContext(DbContextOptions<FieldMonitoringDbContext> options)
        : base(options)
    {
    }

    public DbSet<Field> Fields => Set<Field>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<ProcessedReading> ProcessedReadings => Set<ProcessedReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica todas as configurações deste assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FieldMonitoringDbContext).Assembly);
    }
}
