using FieldMonitoring.Domain.Alerts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldMonitoring.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração do EF Core para a entidade Alert.
/// </summary>
public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("Alerts");

        builder.HasKey(x => x.AlertId);

        builder.Property(x => x.AlertId)
            .ValueGeneratedNever();

        builder.Property(x => x.FarmId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.FieldId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.AlertType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Severity);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(500);

        builder.Property(x => x.StartedAt)
            .IsRequired();

        builder.Property(x => x.ResolvedAt);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(x => new { x.FarmId, x.Status });
        builder.HasIndex(x => new { x.FieldId, x.StartedAt });
        builder.HasIndex(x => new { x.FieldId, x.AlertType, x.Status });
    }
}
