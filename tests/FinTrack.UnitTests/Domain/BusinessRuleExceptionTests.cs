using FinTrack.Domain.Exceptions;

namespace FinTrack.UnitTests.Domain;

public class BusinessRuleExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ExposesMessageToUser()
    {
        // Arrange
        const string message = "Сумма операции должна быть больше нуля.";

        // Act
        var exception = new BusinessRuleException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }
}