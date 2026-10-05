using System.Security.Claims;
using ExpenseTracker.API.Controllers;
using ExpenseTracker.API.Controllers.RequestModels;
using ExpenseTracker.Application.Services;
using ExpenseTracker.Domain.Models;
using ExpenseTracker.UnitTests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ExpenseTracker.UnitTests.API.Controllers;

public class ExpenseControllerTests
{
    private const long CurrentUserId = 42;

    private readonly IExpenseService _expenseService = Substitute.For<IExpenseService>();
    private readonly ExpenseController _controller;

    public ExpenseControllerTests()
    {
        _controller = new ExpenseController(NullLogger<ExpenseController>.Instance, _expenseService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, CurrentUserId.ToString())], "Test"))
                }
            }
        };
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedExpenseForCurrentUserWithExpenseDate()
    {
        // Arrange
        var request = new CreateExpenseRequestModel { Name = "Coffee", Value = 10m, ExpenseDate = new DateTime(2026, 3, 1) };
        _expenseService.CreateExpense(Arg.Any<Expense>()).Returns(call => call.Arg<Expense>());

        // Act
        var result = await _controller.Create(request);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        var expense = objectResult.Value.Should().BeOfType<Expense>().Subject;
        expense.Name.Should().Be("Coffee");
        expense.Value.Should().Be(10m);
        expense.ExpenseDate.Should().Be(new DateTime(2026, 3, 1));
        expense.UserId.Should().Be(CurrentUserId);
    }

    [Fact]
    public async Task GetExpensesByYear_WithNoExpenses_ReturnsOkWithEmptyList()
    {
        // Arrange
        _expenseService.GetExpensesByYear(2026, CurrentUserId).Returns(new List<MonthlyExpenses>());

        // Act
        var result = await _controller.GetExpensesByYear(2026);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<List<MonthlyExpenses>>().Which.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteByIds_WhenExpensesAreDeleted_ReturnsNoContent()
    {
        // Arrange
        long[] ids = [1, 2];
        _expenseService.DeleteByIds(ids, CurrentUserId).Returns(2);

        // Act
        var result = await _controller.DeleteByIds(ids);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteByIds_WhenNoExpenseMatches_ReturnsNotFoundProblem()
    {
        // Arrange
        long[] ids = [99];
        _expenseService.DeleteByIds(ids, CurrentUserId).Returns(0);

        // Act
        var result = await _controller.DeleteByIds(ids);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        objectResult.Value.Should().BeOfType<ProblemDetails>().Which.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1899, false)]
    [InlineData(2201, false)]
    [InlineData(1900, true)]
    [InlineData(2026, true)]
    [InlineData(2200, true)]
    public void GetExpensesByYear_YearParameter_IsValidatedWithinRange(int year, bool expectedValid)
    {
        // Act
        var isValid = ModelValidation.IsValidParameter(
            typeof(ExpenseController).GetMethod(nameof(ExpenseController.GetExpensesByYear))!, "year", year);

        // Assert
        isValid.Should().Be(expectedValid);
    }

    [Fact]
    public void GetExpensesByYear_YearParameter_IsRequired()
    {
        // Arrange
        var parameter = typeof(ExpenseController).GetMethod(nameof(ExpenseController.GetExpensesByYear))!
            .GetParameters().Single(p => p.Name == "year");

        // Act
        var bindRequired = parameter.GetCustomAttribute<BindRequiredAttribute>();

        // Assert
        bindRequired.Should().NotBeNull();
    }

    public static TheoryData<long[]?, bool> IdsCases => new()
    {
        { null, false },
        { [], false },
        { [1], true },
        { [1, 2, 3], true }
    };

    [Theory]
    [MemberData(nameof(IdsCases))]
    public void DeleteByIds_IdsParameter_RequiresAtLeastOneId(long[]? ids, bool expectedValid)
    {
        // Act
        var isValid = ModelValidation.IsValidParameter(
            typeof(ExpenseController).GetMethod(nameof(ExpenseController.DeleteByIds))!, "ids", ids);

        // Assert
        isValid.Should().Be(expectedValid);
    }
}
