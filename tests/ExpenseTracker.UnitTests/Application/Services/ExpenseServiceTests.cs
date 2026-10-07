using ExpenseTracker.Application.IRepositories;
using ExpenseTracker.Application.Services;
using ExpenseTracker.Domain.Models;
using NSubstitute;

namespace ExpenseTracker.UnitTests.Application.Services;

public class ExpenseServiceTests
{
    [Fact]
    public async Task GetExpensesByYear_WithYearAndUserId_ReturnsRepositoryResult()
    {
        // Arrange
        const int year = 2026;
        const long userId = 42;
        var expected = new List<MonthlyExpenses>
        {
            new([new Expense(10m, "Coffee", new DateTime(year, 3, 1), userId)], 3)
        };
        var repository = Substitute.For<IExpenseRepository>();
        repository.GetExpensesByYear(year, userId).Returns(expected);
        var service = new ExpenseService(repository);

        // Act
        var result = await service.GetExpensesByYear(year, userId);

        // Assert
        result.Should().BeSameAs(expected);
        await repository.Received(1).GetExpensesByYear(year, userId);
    }

    [Fact]
    public async Task CreateExpense_WithExpense_ReturnsRepositoryResult()
    {
        // Arrange
        var expense = new Expense(10m, "Coffee", new DateTime(2026, 3, 1), 42);
        var created = new Expense(10m, "Coffee", new DateTime(2026, 3, 1), 42) { Id = 1 };
        var repository = Substitute.For<IExpenseRepository>();
        repository.CreateExpense(expense).Returns(created);
        var service = new ExpenseService(repository);

        // Act
        var result = await service.CreateExpense(expense);

        // Assert
        result.Should().BeSameAs(created);
    }

    [Fact]
    public async Task DeleteByIds_WithIdsAndUserId_ReturnsDeletedCount()
    {
        // Arrange
        long[] ids = [1, 2];
        var repository = Substitute.For<IExpenseRepository>();
        repository.DeleteByIds(ids, 42).Returns(2);
        var service = new ExpenseService(repository);

        // Act
        var result = await service.DeleteByIds(ids, 42);

        // Assert
        result.Should().Be(2);
    }
}
