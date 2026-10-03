using System.Text.Json;
using ExpenseTracker.API.Controllers.RequestModels;
using ExpenseTracker.UnitTests.TestHelpers;

namespace ExpenseTracker.UnitTests.API.Controllers.RequestModels;

public class CreateExpenseRequestModelTests
{
    private static CreateExpenseRequestModel ValidModel() => new()
    {
        Name = "Coffee",
        Value = 10m,
        ExpenseDate = new DateTime(2026, 3, 1)
    };

    [Fact]
    public void Deserialize_WithExpenseDate_SetsExpenseDate()
    {
        // Arrange
        const string json = """{"name":"Coffee","value":10,"expenseDate":"2026-03-01T00:00:00"}""";

        // Act
        var model = JsonSerializer.Deserialize<CreateExpenseRequestModel>(json, JsonSerializerOptions.Web)!;

        // Assert
        model.ExpenseDate.Should().Be(new DateTime(2026, 3, 1));
    }

    [Fact]
    public void Deserialize_WithLegacyDate_SetsExpenseDate()
    {
        // Arrange
        const string json = """{"name":"Coffee","value":10,"date":"2026-03-01T00:00:00"}""";

        // Act
        var model = JsonSerializer.Deserialize<CreateExpenseRequestModel>(json, JsonSerializerOptions.Web)!;

        // Assert
        model.ExpenseDate.Should().Be(new DateTime(2026, 3, 1));
    }

    [Theory]
    [InlineData("""{"name":"Coffee","value":10,"expenseDate":"2026-03-01T00:00:00","date":"2025-01-01T00:00:00"}""")]
    [InlineData("""{"name":"Coffee","value":10,"date":"2025-01-01T00:00:00","expenseDate":"2026-03-01T00:00:00"}""")]
    public void Deserialize_WithExpenseDateAndLegacyDate_PrefersExpenseDate(string json)
    {
        // Act
        var model = JsonSerializer.Deserialize<CreateExpenseRequestModel>(json, JsonSerializerOptions.Web)!;

        // Assert
        model.ExpenseDate.Should().Be(new DateTime(2026, 3, 1));
    }

    [Fact]
    public void Validate_WithValidModel_ReturnsNoErrors()
    {
        // Act
        var results = ModelValidation.Validate(ValidModel());

        // Assert
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithBlankName_ReturnsNameError(string? name)
    {
        // Arrange
        var model = ValidModel();
        model.Name = name!;

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(CreateExpenseRequestModel.Name));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_WithNonPositiveValue_ReturnsValueError(decimal value)
    {
        // Arrange
        var model = ValidModel();
        model.Value = value;

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(CreateExpenseRequestModel.Value));
    }

    [Fact]
    public void Validate_WithMissingDate_ReturnsExpenseDateError()
    {
        // Arrange
        var model = ValidModel();
        model.ExpenseDate = null;

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(CreateExpenseRequestModel.ExpenseDate));
    }

    [Fact]
    public void Validate_WithDefaultDate_ReturnsExpenseDateError()
    {
        // Arrange
        var model = ValidModel();
        model.ExpenseDate = default(DateTime);

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(CreateExpenseRequestModel.ExpenseDate));
    }
}
