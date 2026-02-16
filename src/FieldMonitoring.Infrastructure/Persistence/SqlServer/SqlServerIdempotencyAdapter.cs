using FieldMonitoring.Application.Telemetry;
using FieldMonitoring.Domain.Telemetry;
using Microsoft.Data.SqlClient;
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
        try
        {
            _dbContext.ProcessedReadings.Add(processedReading);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            // Chave duplicada: leitura já foi marcada por outra execução concorrente.
        }
    }

    private static bool IsDuplicateKey(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException
               && (sqlException.Number == 2627 || sqlException.Number == 2601);
    }
}
