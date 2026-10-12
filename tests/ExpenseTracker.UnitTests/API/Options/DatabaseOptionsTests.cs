using ExpenseTracker.API.Options;
using ExpenseTracker.UnitTests.TestHelpers;
using Microsoft.Extensions.Configuration;

namespace ExpenseTracker.UnitTests.API.Options;

public class DatabaseOptionsTests
{
    private const string ValidConnectionString = "Host=localhost;Database=ExpenseTracker;Username=app;Password=secret;";

    [Fact]
    public void Load_WithConnectionStringsSection_UsesIt()
    {
        // Arrange
        var configuration = BuildConfiguration(new()
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=primary",
            ["SqlConnectionString"] = "Host=legacy"
        });
        var options = new DatabaseOptions();

        // Act
        options.Load(configuration);

        // Assert
        options.ConnectionString.Should().Be("Host=primary");
    }

    [Fact]
    public void Load_WithOnlyLegacyVariable_FallsBackToIt()
    {
        // Arrange
        var configuration = BuildConfiguration(new() { ["SqlConnectionString"] = "Host=legacy" });
        var options = new DatabaseOptions();

        // Act
        options.Load(configuration);

        // Assert
        options.ConnectionString.Should().Be("Host=legacy");
    }

    [Fact]
    public void Load_WithNothingConfigured_LeavesConnectionStringEmpty()
    {
        // Arrange
        var configuration = BuildConfiguration(new());
        var options = new DatabaseOptions();

        // Act
        options.Load(configuration);

        // Assert
        options.ConnectionString.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithValidConnectionString_ReturnsNoErrors()
    {
        // Arrange
        var options = new DatabaseOptions { ConnectionString = ValidConnectionString };

        // Act
        var results = ModelValidation.Validate(options);

        // Assert
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingConnectionString_ReturnsRequiredError(string connectionString)
    {
        // Arrange
        var options = new DatabaseOptions { ConnectionString = connectionString };

        // Act
        var results = ModelValidation.Validate(options);

        // Assert
        results.Should().ContainSingle().Which.ErrorMessage.Should().Contain("ConnectionStrings__DefaultConnection");
    }

    [Theory]
    [InlineData("not a connection string")]
    [InlineData("Unknown=value")]
    [InlineData("Database=ExpenseTracker;Username=app")]
    public void Validate_WithInvalidConnectionString_ReturnsErrorWithoutLeakingValue(string connectionString)
    {
        // Arrange
        var options = new DatabaseOptions { ConnectionString = connectionString };

        // Act
        var results = ModelValidation.Validate(options);

        // Assert
        var error = results.Should().ContainSingle().Which;
        error.MemberNames.Should().Equal(nameof(DatabaseOptions.ConnectionString));
        error.ErrorMessage.Should().NotContain(connectionString);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
