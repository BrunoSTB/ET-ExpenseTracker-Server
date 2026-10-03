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
    public void Validate_WithValidModel_ReturnsNoErrors()
    {
        // Act
        var results = ModelValidation.Validate(ValidModel());

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithBoundaryValues_ReturnsNoErrors()
    {
        // Arrange
        var model = ValidModel();
        model.Value = 999_000_000_000m;
        model.ExpenseDate = new DateTime(2200, 12, 31);

        // Act
        var results = ModelValidation.Validate(model);

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
    [InlineData(999_000_000_000.01)]
    public void Validate_WithValueOutOfRange_ReturnsValueError(decimal value)
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

    public static TheoryData<DateTime> OutOfRangeDates => new()
    {
        default(DateTime),
        new DateTime(1899, 12, 31),
        new DateTime(2201, 1, 1)
    };

    [Theory]
    [MemberData(nameof(OutOfRangeDates))]
    public void Validate_WithDateOutOfRange_ReturnsExpenseDateError(DateTime date)
    {
        // Arrange
        var model = ValidModel();
        model.ExpenseDate = date;

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(CreateExpenseRequestModel.ExpenseDate));
    }
}
