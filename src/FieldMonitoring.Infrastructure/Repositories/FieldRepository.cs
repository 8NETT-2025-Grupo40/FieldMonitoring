using FieldMonitoring.Application.Fields;
using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Fields;
using FieldMonitoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FieldMonitoring.Infrastructure.Repositories;

/// <summary>
/// Implementação do repository que persiste o Field aggregate.
/// Usa EF Core change tracking para Field e Alerts.
/// </summary>
public class FieldRepository : IFieldRepository
{
    private readonly FieldMonitoringDbContext _context;

    public FieldRepository(FieldMonitoringDbContext context)
    {
        _context = context;
    }

    public async Task<Field?> GetByIdAsync(string fieldId, CancellationToken cancellationToken)
    {
        Field? field = await _context.Fields
            .Include(f => f.Alerts.Where(a => a.Status == AlertStatus.Active))
            .FirstOrDefaultAsync(f => f.FieldId == fieldId, cancellationToken);

        if (field == null)
            return null;

        field.Rehydrate();

        return field;
    }

    public async Task SaveAsync(Field field, CancellationToken cancellationToken)
    {
        if (_context.Entry(field).State == EntityState.Detached)
        {
            bool exists = await _context.Fields
                .AnyAsync(f => f.FieldId == field.FieldId, cancellationToken);

            if (exists)
            {
                _context.Fields.Attach(field);
                _context.Entry(field).State = EntityState.Modified;
            }
            else
            {
                _context.Fields.Add(field);
            }
        }

        // Batch fetch dos IDs de alertas existentes para evitar N+1 queries
        var alertIds = field.Alerts.Select(a => a.AlertId).ToList();
        var existingAlertIds = (await _context.Alerts
            .Where(a => alertIds.Contains(a.AlertId))
            .Select(a => a.AlertId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        foreach (Alert alert in field.Alerts)
        {
            var alertEntry = _context.Entry(alert);
            if (alertEntry.State == EntityState.Detached)
            {
                _context.Alerts.Attach(alert);
                alertEntry.State = existingAlertIds.Contains(alert.AlertId)
                    ? EntityState.Modified
                    : EntityState.Added;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Obtém todos os talhões de uma fazenda (usado para queries de leitura).
    /// </summary>
    public async Task<IReadOnlyList<Field>> GetByFarmAsync(string farmId, CancellationToken cancellationToken)
    {
        return await _context.Fields
            .AsNoTracking()
            .Where(f => f.FarmId == farmId)
            .ToListAsync(cancellationToken);
    }
}
