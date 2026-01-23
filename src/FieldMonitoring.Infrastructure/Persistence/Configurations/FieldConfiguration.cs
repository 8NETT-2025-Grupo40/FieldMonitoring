using FieldMonitoring.Domain.Fields;
using FieldMonitoring.Domain.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldMonitoring.Infrastructure.Persistence.Configurations;

public class FieldConfiguration : IEntityTypeConfiguration<Field>
{
    public void Configure(EntityTypeBuilder<Field> builder)
    {
        builder.ToTable("Fields");
        

        builder.HasKey(f => f.FieldId);
        builder.Property(f => f.FieldId)
            .IsRequired()
            .HasMaxLength(100);


        builder.Property(f => f.FarmId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.SensorId)
            .HasMaxLength(100);

        builder.Property(f => f.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(f => f.StatusReason)
            .HasMaxLength(500);

        builder.Property(f => f.UpdatedAt)
            .IsRequired();

        builder.Property(f => f.LastReadingAt);

        builder.Property(f => f.LastSoilMoisture)
            .HasConversion(
                v => v != null ? v.Percent : (double?)null,
                v => v.HasValue ? SoilMoisture.FromPercent(v.Value).Value : null
            );

        builder.Property(f => f.LastSoilTemperature)
            .HasConversion(
                v => v != null ? v.Celsius : (double?)null,
                v => v.HasValue ? Temperature.FromCelsius(v.Value).Value : null
            );

        builder.Property(f => f.LastAirTemperature)
            .HasConversion(
                v => v != null ? v.Celsius : (double?)null,
                v => v.HasValue ? Temperature.FromCelsius(v.Value).Value : null
            );

        builder.Property(f => f.LastAirHumidity)
            .HasConversion(
                v => v != null ? v.Percent : (double?)null,
                v => v.HasValue ? AirHumidity.FromPercent(v.Value).Value : null
            );

        builder.Property(f => f.LastRain)
            .HasConversion(
                v => v != null ? v.Millimeters : (double?)null,
                v => v.HasValue ? RainMeasurement.FromMillimeters(v.Value).Value : null
            );

        builder.Property(f => f.LastTimeAboveDryThreshold);
        builder.Property(f => f.LastTimeBelowHeatThreshold);
        builder.Property(f => f.LastTimeAboveFrostThreshold);
        builder.Property(f => f.LastTimeAboveDryAirThreshold);
        builder.Property(f => f.LastTimeBelowHumidAirThreshold);

        builder.HasMany(f => f.Alerts)
            .WithOne()
            .HasForeignKey(a => a.FieldId)
            .HasPrincipalKey(f => f.FieldId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(f => f.Alerts)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(f => f.FarmId);

        builder.HasIndex(f => f.Status);

        builder.HasIndex(f => new { f.FarmId, f.Status });
    }
}
