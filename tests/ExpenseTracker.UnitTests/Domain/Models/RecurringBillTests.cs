using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.UnitTests.Domain.Models;

public class RecurringBillTests
{
    [Theory]
    [InlineData(31, 2026, 2, 28)]
    [InlineData(31, 2028, 2, 29)]
    [InlineData(31, 2026, 4, 30)]
    [InlineData(30, 2026, 2, 28)]
    [InlineData(31, 2026, 1, 31)]
    [InlineData(15, 2026, 2, 15)]
    public void DueDateIn_WithDueDay_UsesLastDayWhenMonthIsShorter(int dueDay, int year, int month, int expectedDay)
    {
        // Arrange
        var recurringBill = new RecurringBill(1, "Rent", dueDay, AmountType.Fixed, new DateOnly(2026, 1, 1));

        // Act
        var result = recurringBill.DueDateIn(new DateOnly(year, month, 1));

        // Assert
        result.Should().Be(new DateOnly(year, month, expectedDay));
    }

    [Theory]
    [InlineData(2026, 2, false)]
    [InlineData(2026, 3, true)]
    [InlineData(2026, 7, true)]
    [InlineData(2026, 8, false)]
    public void IsActiveIn_WithStartAndEndMonth_IncludesStartAndExcludesEnd(int year, int month, bool expected)
    {
        // Arrange
        var recurringBill = new RecurringBill(1, "Rent", 10, AmountType.Fixed, new DateOnly(2026, 3, 1))
        {
            EndMonth = new DateOnly(2026, 8, 1)
        };

        // Act
        var result = recurringBill.IsActiveIn(new DateOnly(year, month, 20));

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsActiveIn_WithoutEndMonth_IsActiveForever()
    {
        // Arrange
        var recurringBill = new RecurringBill(1, "Rent", 10, AmountType.Fixed, new DateOnly(2026, 3, 1));

        // Act
        var result = recurringBill.IsActiveIn(new DateOnly(2099, 12, 1));

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(5, false)]
    [InlineData(4, true)]
    [InlineData(3, true)]
    public void IsEndedBy_WithEndMonth_IsEndedFromEndMonthOn(int? endMonth, bool expected)
    {
        // Arrange
        var recurringBill = new RecurringBill(1, "Rent", 10, AmountType.Fixed, new DateOnly(2026, 1, 1))
        {
            EndMonth = endMonth == null ? null : new DateOnly(2026, endMonth.Value, 1)
        };

        // Act
        var result = recurringBill.IsEndedBy(new DateOnly(2026, 4, 15));

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void CreateBillFor_WithFixedAmount_CopiesFixedValue()
    {
        // Arrange
        var recurringBill = new RecurringBill(7, "Rent", 31, AmountType.Fixed, new DateOnly(2026, 1, 1))
        {
            Id = 3,
            FixedValue = 1500m
        };

        // Act
        var result = recurringBill.CreateBillFor(new DateOnly(2026, 2, 1));

        // Assert
        result.UserId.Should().Be(7);
        result.RecurringBillId.Should().Be(3);
        result.Name.Should().Be("Rent");
        result.AmountType.Should().Be(AmountType.Fixed);
        result.DueDate.Should().Be(new DateOnly(2026, 2, 28));
        result.Value.Should().Be(1500m);
        result.PaidAt.Should().BeNull();
    }

    [Theory]
    [InlineData(AmountType.VariableEstimated)]
    [InlineData(AmountType.VariableOnArrival)]
    public void CreateBillFor_WithVariableAmount_LeavesValueEmpty(AmountType amountType)
    {
        // Arrange
        var recurringBill = new RecurringBill(7, "Power", 10, amountType, new DateOnly(2026, 1, 1)) { FixedValue = 100m };

        // Act
        var result = recurringBill.CreateBillFor(new DateOnly(2026, 2, 1));

        // Assert
        result.Value.Should().BeNull();
    }
}
