using FieldMonitoring.Domain.Alerts;

namespace FieldMonitoring.Domain.Tests.Alerts;

public class AlertTests
{
    [Fact]
    public void Create_ShouldCreateActiveAlert()
    {
        // Arrange
        var farmId = "farm-1";
        var fieldId = "field-1";
        var reason = "Soil moisture below 30% for 24 hours";

        // Act
        Alert alert = Alert.Create(AlertType.Dryness, farmId, fieldId, reason);

        // Assert
        alert.FarmId.Should().Be(farmId);
        alert.FieldId.Should().Be(fieldId);
        alert.AlertType.Should().Be(AlertType.Dryness);
        alert.Status.Should().Be(AlertStatus.Active);
        alert.Reason.Should().Be(reason);
        alert.ResolvedAt.Should().BeNull();
    }

    [Fact]
    public void Resolve_ShouldSetStatusToResolved()
    {
        // Arrange
        Alert alert = Alert.Create(AlertType.Dryness, "farm-1", "field-1", "Test");

        // Act
        alert.Resolve();

        // Assert
        alert.Status.Should().Be(AlertStatus.Resolved);
        alert.ResolvedAt.Should().NotBeNull();
        alert.ResolvedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WhenFarmIdIsNull_ShouldThrowException()
    {
        // Act
        Action act = () => Alert.Create(AlertType.Dryness, null!, "field-1", "reason");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WhenFieldIdIsWhitespace_ShouldThrowException()
    {
        // Act
        Action act = () => Alert.Create(AlertType.Dryness, "farm-1", "   ", "reason");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WhenReasonIsWhitespace_ShouldThrowException()
    {
        // Act
        Action act = () => Alert.Create(AlertType.Dryness, "farm-1", "field-1", "   ");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

}
