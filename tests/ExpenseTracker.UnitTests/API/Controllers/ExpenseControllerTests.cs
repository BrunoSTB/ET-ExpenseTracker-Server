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
    public async Task Create_WithValidRequest_CreatesExpenseForCurrentUserWithExpenseDate()
    {
        // Arrange
        var request = new CreateExpenseRequestModel { Name = "Coffee", Value = 10m, ExpenseDate = new DateTime(2026, 3, 1) };
        _expenseService.CreateExpense(Arg.Any<Expense>()).Returns(call => call.Arg<Expense>());

        // Act
        var result = await _controller.Create(request);

        // Assert
        var expense = result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<Expense>().Subject;
        expense.Name.Should().Be("Coffee");
        expense.Value.Should().Be(10m);
        expense.ExpenseDate.Should().Be(new DateTime(2026, 3, 1));
        expense.UserId.Should().Be(CurrentUserId);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1899, false)]
    [InlineData(10000, false)]
    [InlineData(1900, true)]
    [InlineData(2026, true)]
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
