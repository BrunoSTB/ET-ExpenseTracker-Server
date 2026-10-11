using ExpenseTracker.API.Options;
using ExpenseTracker.UnitTests.TestHelpers;

namespace ExpenseTracker.UnitTests.API.Options;

public class JwtOptionsTests
{
    [Fact]
    public void Validate_WithLongEnoughSecret_ReturnsNoErrors()
    {
        // Arrange
        var options = new JwtOptions { Secret = new string('a', JwtOptions.MinSecretBytes) };

        // Act
        var results = ModelValidation.Validate(options);

        // Assert
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("too-short-secret")]
    public void Validate_WithMissingOrShortSecret_ReturnsSecretError(string secret)
    {
        // Arrange
        var options = new JwtOptions { Secret = secret };

        // Act
        var results = ModelValidation.Validate(options);

        // Assert
        results.Should().ContainSingle().Which.ErrorMessage.Should().StartWith("JWT_SECRET");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_WithNonPositiveExpiration_ReturnsExpirationError(int minutes)
    {
        // Arrange
        var options = new JwtOptions { Secret = new string('a', JwtOptions.MinSecretBytes), ExpirationMinutes = minutes };

        // Act
        var results = ModelValidation.Validate(options);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(JwtOptions.ExpirationMinutes));
    }
}
