using ExpenseTracker.API.Options;
using ExpenseTracker.UnitTests.TestHelpers;
using Microsoft.Extensions.Configuration;

namespace ExpenseTracker.UnitTests.API.Options;

public class CorsOptionsTests
{
    [Fact]
    public void Load_WithCommaSeparatedOrigins_SplitsAndTrimsThem()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CORSOrigins"] = " http://localhost:4200 ,, https://app.example.com"
            })
            .Build();
        var options = new CorsOptions();

        // Act
        options.Load(configuration);

        // Assert
        options.Origins.Should().Equal("http://localhost:4200", "https://app.example.com");
    }

    [Fact]
    public void Load_WithoutVariable_LeavesOriginsEmpty()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var options = new CorsOptions();

        // Act
        options.Load(configuration);

        // Assert
        options.Origins.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithValidOrigins_ReturnsNoErrors()
    {
        // Arrange
        var options = new CorsOptions { Origins = ["http://localhost:4200", "https://app.example.com"] };

        // Act
        var results = ModelValidation.Validate(options);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithNoOrigins_ReturnsRequiredError()
    {
        // Arrange
        var options = new CorsOptions();

        // Act
        var results = ModelValidation.Validate(options);

        // Assert
        results.Should().ContainSingle().Which.ErrorMessage.Should().StartWith("CORSOrigins is required");
    }

    [Theory]
    [InlineData("localhost:4200")]
    [InlineData("ftp://example.com")]
    [InlineData("http://localhost:4200/")]
    [InlineData("https://example.com/app")]
    [InlineData("https://example.com?x=1")]
    public void Validate_WithInvalidOrigin_ReturnsErrorNamingIt(string origin)
    {
        // Arrange
        var options = new CorsOptions { Origins = ["http://localhost:4200", origin] };

        // Act
        var results = ModelValidation.Validate(options);

        // Assert
        results.Should().ContainSingle().Which.ErrorMessage.Should().Contain($"'{origin}'");
    }
}
