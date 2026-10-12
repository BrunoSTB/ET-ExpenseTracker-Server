using ExpenseTracker.API.Options;
using ExpenseTracker.UnitTests.TestHelpers;
using Microsoft.Extensions.Configuration;

namespace ExpenseTracker.UnitTests.API.Options;

public class JwtOptionsTests
{
    [Fact]
    public void Load_WithAllValuesConfigured_UsesThem()
    {
        // Arrange
        var configuration = BuildConfiguration(new()
        {
            ["JWT_SECRET"] = "configured-secret",
            ["JWT_ISSUER"] = "configured-issuer",
            ["JWT_AUDIENCE"] = "configured-audience",
            ["JWT_EXPIRATION_MINUTES"] = "15"
        });
        var options = new JwtOptions();

        // Act
        options.Load(configuration);

        // Assert
        options.Secret.Should().Be("configured-secret");
        options.Issuer.Should().Be("configured-issuer");
        options.Audience.Should().Be("configured-audience");
        options.ExpirationMinutes.Should().Be(15);
    }

    [Fact]
    public void Load_WithNothingConfigured_KeepsDefaultsAndLeavesSecretEmpty()
    {
        // Arrange
        var configuration = BuildConfiguration(new());
        var options = new JwtOptions();
        var defaults = new JwtOptions();

        // Act
        options.Load(configuration);

        // Assert
        options.Secret.Should().BeEmpty();
        options.Issuer.Should().Be(defaults.Issuer);
        options.Audience.Should().Be(defaults.Audience);
        options.ExpirationMinutes.Should().Be(defaults.ExpirationMinutes);
    }

    [Fact]
    public void Load_WithNonNumericExpiration_SetsInvalidExpiration()
    {
        // Arrange
        var configuration = BuildConfiguration(new() { ["JWT_EXPIRATION_MINUTES"] = "soon" });
        var options = new JwtOptions();

        // Act
        options.Load(configuration);

        // Assert
        options.ExpirationMinutes.Should().Be(0);
    }

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

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
