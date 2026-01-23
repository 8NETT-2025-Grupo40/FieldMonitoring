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

    /// <summary>
    /// Carrega Field aggregate completo com alertas ativos.
    /// EF Core carrega os alertas automaticamente via Include.
    /// </summary>
    public async Task<Field?> GetByIdAsync(string fieldId, CancellationToken cancellationToken)
    {
        Field? field = await _context.Fields
            .Include(f => f.Alerts.Where(a => a.Status == AlertStatus.Active))
            .FirstOrDefaultAsync(f => f.FieldId == fieldId, cancellationToken);

        if (field == null)
            return null;

        // Sincroniza estado interno _dryAlertActive com os alertas carregados
        field.SyncAlertStates();

        return field;
    }

    /// <summary>
    /// Persiste Field aggregate (Field + Alerts).
    /// Usa change tracking do EF Core - SaveChanges é atômico.
    /// </summary>
    public async Task SaveAsync(Field field, CancellationToken cancellationToken)
    {
        // Se Field está detached (novo ou veio de outro contexto)
        if (_context.Entry(field).State == EntityState.Detached)
        {
            // Verifica se já existe no banco
            bool exists = await _context.Fields
                .AnyAsync(f => f.FieldId == field.FieldId, cancellationToken);

            if (exists)
            {
                // Attach e marcar como modificado
                _context.Fields.Attach(field);
                _context.Entry(field).State = EntityState.Modified;
            }
            else
            {
                // Novo Field - Add marca Field e Alerts como Added
                _context.Fields.Add(field);
            }
        }
        // Se não está Detached, o change tracker já sabe o que fazer

        // Para alertas na coleção, garantir estado correto
        foreach (Alert alert in field.Alerts)
        {
            var alertEntry = _context.Entry(alert);
            if (alertEntry.State == EntityState.Detached)
            {
                bool alertExists = await _context.Alerts
                    .AnyAsync(a => a.AlertId == alert.AlertId, cancellationToken);

                _context.Alerts.Attach(alert);
                alertEntry.State = alertExists ? EntityState.Modified : EntityState.Added;
            }
        }

        // SaveChanges persiste Field + Alerts em uma única transação
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
