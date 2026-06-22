using WikiApi.Auth;

namespace WikiApi.Tests;

public class AdminTokenValidatorTests
{
    [Theory]
    [InlineData("wrong-token")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_WrongOrEmptyToken_ReturnsFalse(string? providedToken)
    {
        // Arrange
        var validator = new AdminTokenValidator("my-secret-token");

        // Act
        var result = validator.IsValid(providedToken);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("first-configured-token")]
    [InlineData("second-configured-token")]
    [InlineData("any-custom-token-123")]
    public void IsValid_ConfiguredToken_ReturnsTrue(string configuredToken)
    {
        // Arrange
        var validator = new AdminTokenValidator(configuredToken);

        // Act
        var result = validator.IsValid(configuredToken);

        // Assert
        Assert.True(result);
    }
}
