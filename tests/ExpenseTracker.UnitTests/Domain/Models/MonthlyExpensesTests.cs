using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.UnitTests.Domain.Models;

public class MonthlyExpensesTests
{
    [Fact]
    public void Constructor_WithEmptyList_SetsTotalExpensesToZero()
    {
        // Arrange
        var expenses = new List<Expense>();

        // Act
        var result = new MonthlyExpenses(expenses, 1);

        // Assert
        result.TotalExpenses.Should().Be(0m);
    }

    [Fact]
    public void Constructor_WithDecimalValues_SumsWithoutPrecisionLoss()
    {
        // Arrange
        var expenses = new List<Expense>
        {
            new(0.1m, "Coffee", new DateTime(2026, 1, 1), 1),
            new(0.2m, "Bread", new DateTime(2026, 1, 2), 1)
        };

        // Act
        var result = new MonthlyExpenses(expenses, 1);

        // Assert
        result.TotalExpenses.Should().Be(0.3m);
    }

    [Fact]
    public void Constructor_WhenExpensesChangeAfterConstruction_KeepsOriginalTotal()
    {
        // Arrange
        var expenses = new List<Expense>
        {
            new(10m, "Coffee", new DateTime(2026, 1, 1), 1)
        };
        var monthlyExpenses = new MonthlyExpenses(expenses, 1);

        // Act
        monthlyExpenses.Expenses.Add(new Expense(5m, "Bread", new DateTime(2026, 1, 2), 1));

        // Assert
        monthlyExpenses.TotalExpenses.Should().Be(10m);
    }
}
