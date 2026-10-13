using ExpenseTracker.API.Controllers.RequestModels;
using ExpenseTracker.Domain.Models;
using ExpenseTracker.UnitTests.TestHelpers;

namespace ExpenseTracker.UnitTests.API.Controllers.RequestModels;

public class BillRequestModelTests
{
    private static readonly DateOnly DueDate = new(2026, 12, 24);

    [Fact]
    public void Validate_WithFixedAmountAndValue_ReturnsNoErrors()
    {
        // Arrange
        var model = new BillRequestModel { Name = "Gift", DueDate = DueDate, AmountType = AmountType.Fixed, Value = 50m };

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(AmountType.VariableEstimated)]
    [InlineData(AmountType.VariableOnArrival)]
    public void Validate_WithVariableAmountWithoutValue_ReturnsNoErrors(AmountType amountType)
    {
        // Arrange
        var model = new BillRequestModel { Name = "Repair", DueDate = DueDate, AmountType = amountType };

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithFixedAmountWithoutValue_ReturnsValueError()
    {
        // Arrange
        var model = new BillRequestModel { Name = "Gift", DueDate = DueDate, AmountType = AmountType.Fixed };

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(BillRequestModel.Value));
    }

    [Theory]
    [InlineData(AmountType.VariableEstimated)]
    [InlineData(AmountType.VariableOnArrival)]
    public void Validate_WithVariableAmountAndValue_ReturnsValueError(AmountType amountType)
    {
        // Arrange
        var model = new BillRequestModel { Name = "Repair", DueDate = DueDate, AmountType = amountType, Value = 10m };

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(BillRequestModel.Value));
    }

    [Fact]
    public void Validate_WithNegativeValue_ReturnsValueError()
    {
        // Arrange
        var model = new BillRequestModel { Name = "Gift", DueDate = DueDate, AmountType = AmountType.Fixed, Value = -1m };

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(BillRequestModel.Value));
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(3000)]
    public void Validate_WithDueDateOutOfRange_ReturnsDueDateError(int year)
    {
        // Arrange
        var model = new BillRequestModel { Name = "Gift", DueDate = new DateOnly(year, 1, 1), AmountType = AmountType.VariableOnArrival };

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(BillRequestModel.DueDate));
    }

    [Fact]
    public void Validate_WithoutDueDate_ReturnsDueDateError()
    {
        // Arrange
        var model = new BillRequestModel { Name = "Gift", AmountType = AmountType.VariableOnArrival };

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(BillRequestModel.DueDate));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithBlankName_ReturnsNameError(string? name)
    {
        // Arrange
        var model = new BillRequestModel { Name = name!, DueDate = DueDate, AmountType = AmountType.VariableOnArrival };

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(BillRequestModel.Name));
    }

    [Fact]
    public void Validate_WithoutAmountType_ReturnsAmountTypeError()
    {
        // Arrange
        var model = new BillRequestModel { Name = "Gift", DueDate = DueDate };

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(BillRequestModel.AmountType));
    }
}
