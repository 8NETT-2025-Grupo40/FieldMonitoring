using FieldMonitoring.Application.Telemetry;
using FieldMonitoring.Domain.Telemetry;
using Microsoft.EntityFrameworkCore;

namespace FieldMonitoring.Infrastructure.Persistence.SqlServer;

/// <summary>
/// Implementação SQL Server do IIdempotencyStore.
/// </summary>
public class SqlServerIdempotencyAdapter : IIdempotencyStore
{
    private readonly FieldMonitoringDbContext _dbContext;

    public SqlServerIdempotencyAdapter(FieldMonitoringDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ExistsAsync(string readingId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProcessedReadings
            .AnyAsync(x => x.ReadingId == readingId, cancellationToken);
    }

    public async Task MarkProcessedAsync(ProcessedReading processedReading, CancellationToken cancellationToken = default)
    {
        // Verifica se já existe para evitar exceção de chave duplicada
        var exists = await _dbContext.ProcessedReadings
            .AnyAsync(x => x.ReadingId == processedReading.ReadingId, cancellationToken);

        if (!exists)
        {
            _dbContext.ProcessedReadings.Add(processedReading);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
