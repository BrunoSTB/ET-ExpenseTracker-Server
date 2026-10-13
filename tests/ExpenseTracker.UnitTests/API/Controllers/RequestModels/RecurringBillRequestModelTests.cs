using ExpenseTracker.API.Controllers.RequestModels;
using ExpenseTracker.Domain.Models;
using ExpenseTracker.UnitTests.TestHelpers;

namespace ExpenseTracker.UnitTests.API.Controllers.RequestModels;

public class RecurringBillRequestModelTests
{
    [Fact]
    public void Validate_WithFixedAmountAndValue_ReturnsNoErrors()
    {
        // Arrange
        var model = new RecurringBillRequestModel { Name = "Rent", DueDay = 10, AmountType = AmountType.Fixed, FixedValue = 1500m };

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
        var model = new RecurringBillRequestModel { Name = "Power", DueDay = 10, AmountType = amountType };

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithFixedAmountWithoutValue_ReturnsFixedValueError()
    {
        // Arrange
        var model = new RecurringBillRequestModel { Name = "Rent", DueDay = 10, AmountType = AmountType.Fixed };

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(RecurringBillRequestModel.FixedValue));
    }

    [Theory]
    [InlineData(AmountType.VariableEstimated)]
    [InlineData(AmountType.VariableOnArrival)]
    public void Validate_WithVariableAmountAndValue_ReturnsFixedValueError(AmountType amountType)
    {
        // Arrange
        var model = new RecurringBillRequestModel { Name = "Power", DueDay = 10, AmountType = amountType, FixedValue = 100m };

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(RecurringBillRequestModel.FixedValue));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Validate_WithDueDayOutOfRange_ReturnsDueDayError(int dueDay)
    {
        // Arrange
        var model = new RecurringBillRequestModel { Name = "Rent", DueDay = dueDay, AmountType = AmountType.Fixed, FixedValue = 1m };

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(RecurringBillRequestModel.DueDay));
    }

    [Fact]
    public void Validate_WithNegativeFixedValue_ReturnsFixedValueError()
    {
        // Arrange
        var model = new RecurringBillRequestModel { Name = "Rent", DueDay = 10, AmountType = AmountType.Fixed, FixedValue = -1m };

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(RecurringBillRequestModel.FixedValue));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithBlankName_ReturnsNameError(string? name)
    {
        // Arrange
        var model = new RecurringBillRequestModel { Name = name!, DueDay = 10, AmountType = AmountType.VariableOnArrival };

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(RecurringBillRequestModel.Name));
    }

    [Fact]
    public void Validate_WithoutAmountType_ReturnsAmountTypeError()
    {
        // Arrange
        var model = new RecurringBillRequestModel { Name = "Rent", DueDay = 10 };

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(RecurringBillRequestModel.AmountType));
    }
}
