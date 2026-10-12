using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ExpenseTracker.Domain.Models;
using ExpenseTracker.IntegrationTests.Infrastructure;
using NSubstitute;

namespace ExpenseTracker.IntegrationTests.Api;

public class ExpenseEndpointsTests : IAsyncDisposable
{
    private const long UserId = 42;
    private const string ProblemJson = "application/problem+json";

    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public ExpenseEndpointsTests()
    {
        _client = _factory.CreateApiClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetExpenses_WithoutToken_ReturnsUnauthorizedProblem()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/Expense?year=2026");

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
    }

    public static TheoryData<string> InvalidTokens() => new()
    {
        TestTokens.WithWrongIssuer(UserId),
        TestTokens.WithWrongAudience(UserId),
        TestTokens.Expired(UserId),
        TestTokens.WithOtherKey(UserId)
    };

    [Theory]
    [MemberData(nameof(InvalidTokens))]
    public async Task GetExpenses_WithInvalidToken_ReturnsUnauthorized(string token)
    {
        // Arrange
        var request = Authorized(HttpMethod.Get, "/Expense?year=2026", token);

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await _factory.ExpenseRepository.DidNotReceiveWithAnyArgs().GetExpensesByYear(default, default);
    }

    [Fact]
    public async Task GetExpenses_WithToken_ReturnsOkWithUserIdFromClaim()
    {
        // Arrange
        var expenses = new List<MonthlyExpenses>
        {
            new([new Expense(12.5m, "Coffee", new DateTime(2026, 3, 1), UserId)], 3)
        };
        _factory.ExpenseRepository.GetExpensesByYear(2026, UserId).Returns(expenses);
        var request = Authorized(HttpMethod.Get, "/Expense?year=2026", ValidToken());

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Contain("Coffee");
        await _factory.ExpenseRepository.Received(1).GetExpensesByYear(2026, UserId);
    }

    [Theory]
    [InlineData("/Expense")]
    [InlineData("/Expense?year=1899")]
    [InlineData("/Expense?year=2201")]
    public async Task GetExpenses_WithMissingOrOutOfRangeYear_ReturnsValidationProblem(string url)
    {
        // Arrange
        var request = Authorized(HttpMethod.Get, url, ValidToken());

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
        await _factory.ExpenseRepository.DidNotReceiveWithAnyArgs().GetExpensesByYear(default, default);
    }

    [Fact]
    public async Task CreateExpense_WithValidBody_ReturnsCreatedForUserFromClaim()
    {
        // Arrange
        _factory.ExpenseRepository.CreateExpense(Arg.Any<Expense>())
            .Returns(call =>
            {
                var expense = call.Arg<Expense>();
                expense.Id = 10;
                return expense;
            });
        var request = Authorized(HttpMethod.Post, "/Expense", ValidToken());
        request.Content = JsonContent.Create(new { name = "Lunch", value = 25.90m, expenseDate = "2026-05-10" });

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        await _factory.ExpenseRepository.Received(1).CreateExpense(Arg.Is<Expense>(e =>
            e.Name == "Lunch" && e.Value == 25.90m && e.ExpenseDate == new DateTime(2026, 5, 10) && e.UserId == UserId));
    }

    [Fact]
    public async Task CreateExpense_WithInvalidBody_ReturnsValidationProblem()
    {
        // Arrange
        var request = Authorized(HttpMethod.Post, "/Expense", ValidToken());
        request.Content = JsonContent.Create(new { value = 0m });

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
        await _factory.ExpenseRepository.DidNotReceiveWithAnyArgs().CreateExpense(default!);
    }

    [Fact]
    public async Task DeleteByIds_WhenExpensesDeleted_ReturnsNoContent()
    {
        // Arrange
        _factory.ExpenseRepository.DeleteByIds(Arg.Is<long[]>(ids => ids.SequenceEqual(new long[] { 1, 2 })), UserId)
            .Returns(2);
        var request = Authorized(HttpMethod.Delete, "/Expense/DeleteByIds?ids=1&ids=2", ValidToken());

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await _factory.ExpenseRepository.Received(1)
            .DeleteByIds(Arg.Is<long[]>(ids => ids.SequenceEqual(new long[] { 1, 2 })), UserId);
    }

    [Fact]
    public async Task DeleteByIds_WhenNothingDeleted_ReturnsNotFoundProblem()
    {
        // Arrange
        _factory.ExpenseRepository.DeleteByIds(Arg.Any<long[]>(), UserId).Returns(0);
        var request = Authorized(HttpMethod.Delete, "/Expense/DeleteByIds?ids=99", ValidToken());

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
    }

    [Fact]
    public async Task DeleteByIds_WithoutIds_ReturnsValidationProblem()
    {
        // Arrange
        var request = Authorized(HttpMethod.Delete, "/Expense/DeleteByIds", ValidToken());

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
        await _factory.ExpenseRepository.DidNotReceiveWithAnyArgs().DeleteByIds(default!, default);
    }

    private string ValidToken()
    {
        return TestTokens.For(_factory, new User("bruno") { Id = UserId });
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}
