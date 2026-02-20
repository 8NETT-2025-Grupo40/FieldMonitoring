using FieldMonitoring.Domain.Alerts;

namespace FieldMonitoring.Domain.Tests.Alerts;

public class AlertTypeExtensionsTests
{
    [Fact]
    public void GetSeverity_WhenKnownType_ShouldReturnExpectedValue()
    {
        // Act
        int severity = AlertType.Frost.GetSeverity();

        // Assert
        severity.Should().Be(1);
    }

    [Fact]
    public void GetSeverity_WhenUnknownType_ShouldThrowException()
    {
        // Arrange
        AlertType unknownType = (AlertType)999;

        // Act
        Action act = () => unknownType.GetSeverity();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ToFieldStatus_WhenUnknownType_ShouldThrowException()
    {
        // Arrange
        AlertType unknownType = (AlertType)999;

        // Act
        Action act = () => unknownType.ToFieldStatus();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetDefaultReason_WhenUnknownType_ShouldThrowException()
    {
        // Arrange
        AlertType unknownType = (AlertType)999;

        // Act
        Action act = () => unknownType.GetDefaultReason();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
