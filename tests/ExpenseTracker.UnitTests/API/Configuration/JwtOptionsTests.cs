using ExpenseTracker.API.Configuration;
using ExpenseTracker.UnitTests.TestHelpers;

namespace ExpenseTracker.UnitTests.API.Configuration;

public class JwtOptionsTests
{
    [Fact]
    public void Validate_WithValidOptions_ReturnsNoErrors()
    {
        // Arrange
        var options = CreateValidOptions();

        // Act
        var results = ModelValidation.Validate(options);

        // Assert
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithBlankSecret_ReturnsSecretError(string secret)
    {
        // Arrange
        var options = CreateValidOptions();
        options.Secret = secret;

        // Act
        var results = ModelValidation.Validate(options);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(JwtOptions.Secret));
    }

    [Fact]
    public void Validate_WithSecretShorterThan32Bytes_ReturnsSecretError()
    {
        // Arrange
        var options = CreateValidOptions();
        options.Secret = new string('a', JwtOptions.MinimumSecretBytes - 1);

        // Act
        var results = ModelValidation.Validate(options);

        // Assert
        var error = results.Should().ContainSingle().Subject;
        error.MemberNames.Should().Equal(nameof(JwtOptions.Secret));
        error.ErrorMessage.Should().Contain("32 bytes");
    }

    [Fact]
    public void Validate_WithSecretOf32Bytes_ReturnsNoErrors()
    {
        // Arrange
        var options = CreateValidOptions();
        options.Secret = new string('a', JwtOptions.MinimumSecretBytes);

        // Act
        var results = ModelValidation.Validate(options);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithBlankIssuer_ReturnsIssuerError()
    {
        // Arrange
        var options = CreateValidOptions();
        options.Issuer = "";

        // Act
        var results = ModelValidation.Validate(options);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(JwtOptions.Issuer));
    }

    [Fact]
    public void Validate_WithBlankAudience_ReturnsAudienceError()
    {
        // Arrange
        var options = CreateValidOptions();
        options.Audience = "";

        // Act
        var results = ModelValidation.Validate(options);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(JwtOptions.Audience));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(JwtOptions.MaximumExpirationMinutes + 1)]
    public void Validate_WithExpirationOutOfRange_ReturnsExpirationError(int expirationMinutes)
    {
        // Arrange
        var options = CreateValidOptions();
        options.ExpirationMinutes = expirationMinutes;

        // Act
        var results = ModelValidation.Validate(options);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(JwtOptions.ExpirationMinutes));
    }

    private static JwtOptions CreateValidOptions()
    {
        return new JwtOptions
        {
            Secret = "a-test-secret-that-is-at-least-32-bytes-long",
            Issuer = "expense-tracker",
            Audience = "expense-tracker-client",
            ExpirationMinutes = 60
        };
    }
}
