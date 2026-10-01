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
}
