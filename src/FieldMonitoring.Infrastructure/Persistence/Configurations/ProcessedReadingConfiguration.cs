using FieldMonitoring.Domain.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldMonitoring.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração do EF Core para a entidade ProcessedReading.
/// </summary>
public class ProcessedReadingConfiguration : IEntityTypeConfiguration<ProcessedReading>
{
    public void Configure(EntityTypeBuilder<ProcessedReading> builder)
    {
        builder.ToTable("ProcessedReadings");

        builder.HasKey(x => x.ReadingId);

        builder.Property(x => x.ReadingId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.FieldId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .IsRequired();

        builder.Property(x => x.Source)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Índice para consultas por talhão
        builder.HasIndex(x => x.FieldId);
    }
}
