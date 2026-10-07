using ExpenseTracker.API.Configuration;
using ExpenseTracker.UnitTests.TestHelpers;

namespace ExpenseTracker.UnitTests.API.Configuration;

public class CorsOptionsTests
{
    [Theory]
    [InlineData("http://localhost:4200")]
    [InlineData("https://app.example.com")]
    [InlineData("http://localhost:4200, https://app.example.com")]
    public void Validate_WithValidOrigins_ReturnsNoErrors(string allowedOrigins)
    {
        // Arrange
        var options = new CorsOptions { AllowedOrigins = allowedOrigins };

        // Act
        var results = ModelValidation.Validate(options);

        // Assert
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithBlankOrigins_ReturnsAllowedOriginsError(string allowedOrigins)
    {
        // Arrange
        var options = new CorsOptions { AllowedOrigins = allowedOrigins };

        // Act
        var results = ModelValidation.Validate(options);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(CorsOptions.AllowedOrigins));
    }

    [Theory]
    [InlineData("http://localhost:4200/")]
    [InlineData("https://app.example.com/path")]
    [InlineData("localhost:4200")]
    [InlineData("ftp://app.example.com")]
    [InlineData("http://localhost:4200,not-an-origin")]
    public void Validate_WithInvalidOrigin_ReturnsAllowedOriginsError(string allowedOrigins)
    {
        // Arrange
        var options = new CorsOptions { AllowedOrigins = allowedOrigins };

        // Act
        var results = ModelValidation.Validate(options);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(CorsOptions.AllowedOrigins));
    }

    [Fact]
    public void GetOrigins_WithCommaSeparatedValue_ReturnsTrimmedNonEmptyOrigins()
    {
        // Arrange
        var options = new CorsOptions { AllowedOrigins = " http://localhost:4200 ,, https://app.example.com " };

        // Act
        var origins = options.GetOrigins();

        // Assert
        origins.Should().Equal("http://localhost:4200", "https://app.example.com");
    }
}
