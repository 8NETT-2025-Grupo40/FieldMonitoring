using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Domain.Alerts;
using Microsoft.EntityFrameworkCore;

namespace FieldMonitoring.Infrastructure.Persistence.SqlServer;

/// <summary>
/// Implementação SQL Server do IAlertStore.
/// </summary>
public class SqlServerAlertAdapter : IAlertStore
{
    private readonly FieldMonitoringDbContext _dbContext;

    public SqlServerAlertAdapter(FieldMonitoringDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Alert>> GetActiveByFarmAsync(string farmId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Alerts.AsNoTracking()
            .Where(x => x.FarmId == farmId && x.Status == AlertStatus.Active)
            .OrderByDescending(x => x.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Alert>> GetActiveByFieldAsync(string fieldId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Alerts.AsNoTracking()
            .Where(x => x.FieldId == fieldId && x.Status == AlertStatus.Active)
            .OrderByDescending(x => x.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Alert>> GetByFieldAsync(
        string fieldId, 
        DateTimeOffset? from = null, 
        DateTimeOffset? to = null, 
        CancellationToken cancellationToken = default)
    {
        IQueryable<Alert> query = _dbContext.Alerts.AsNoTracking()
            .Where(x => x.FieldId == fieldId);

        if (from.HasValue)
            query = query.Where(x => x.StartedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.StartedAt <= to.Value);

        return await query
            .OrderByDescending(x => x.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Alert>> GetByFarmAsync(
        string farmId, 
        DateTimeOffset? from = null, 
        DateTimeOffset? to = null, 
        CancellationToken cancellationToken = default)
    {
        IQueryable<Alert> query = _dbContext.Alerts.AsNoTracking()
            .Where(x => x.FarmId == farmId);

        if (from.HasValue)
            query = query.Where(x => x.StartedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.StartedAt <= to.Value);

        return await query
            .OrderByDescending(x => x.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Alert?> GetByIdAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Alerts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.AlertId == alertId, cancellationToken);
    }

    public async Task<int> CountActiveByFieldAsync(string fieldId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Alerts
            .CountAsync(x => x.FieldId == fieldId && x.Status == AlertStatus.Active, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, int>> CountActiveByFieldsAsync(
        IEnumerable<string> fieldIds, CancellationToken cancellationToken = default)
    {
        var fieldIdList = fieldIds.ToList();

        var counts = await _dbContext.Alerts
            .Where(a => fieldIdList.Contains(a.FieldId) && a.Status == AlertStatus.Active)
            .GroupBy(a => a.FieldId)
            .Select(g => new { FieldId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.FieldId, x => x.Count);
    }
}
