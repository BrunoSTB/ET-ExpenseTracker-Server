using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExpenseTracker.Domain.Models;
using ExpenseTracker.IntegrationTests.Infrastructure;
using NSubstitute;

namespace ExpenseTracker.IntegrationTests.Api;

public class RecurringBillEndpointsTests : IAsyncDisposable
{
    private const long UserId = 7;
    private const long OtherUserId = 8;
    private const string ProblemJson = "application/problem+json";
    private static readonly DateOnly CurrentMonth = new(2026, 10, 1);

    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public RecurringBillEndpointsTests()
    {
        _client = _factory.CreateApiClient();
        _factory.RecurringBillRepository.Create(Arg.Any<RecurringBill>())
            .Returns(call =>
            {
                var recurringBill = call.Arg<RecurringBill>();
                recurringBill.Id = 3;
                return recurringBill;
            });
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetActive_WithoutToken_ReturnsUnauthorizedProblem()
    {
        // Act
        var response = await _client.GetAsync("/RecurringBill", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
    }

    [Fact]
    public async Task GetActive_WithToken_ReturnsUserRecurringBillsFromCurrentMonth()
    {
        // Arrange
        _factory.RecurringBillRepository.GetActive(UserId, CurrentMonth).Returns([Stored(UserId)]);

        // Act
        var response = await SendAsync(HttpMethod.Get, "/RecurringBill", UserId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ReadJsonAsync(response);
        json.GetArrayLength().Should().Be(1);
        var item = json[0];
        item.GetProperty("id").GetInt64().Should().Be(3);
        item.GetProperty("name").GetString().Should().Be("Rent");
        item.GetProperty("dueDay").GetInt32().Should().Be(10);
        item.GetProperty("amountType").GetString().Should().Be("Fixed");
        item.GetProperty("fixedValue").GetDecimal().Should().Be(1500m);
        item.GetProperty("startMonth").GetString().Should().Be("2026-01-01");
        item.GetProperty("endMonth").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Create_WithValidBody_ReturnsCreatedForCurrentUser()
    {
        // Arrange
        var body = new { name = "Rent", dueDay = 31, amountType = "Fixed", fixedValue = 1500m, userId = OtherUserId };

        // Act
        var response = await SendAsync(HttpMethod.Post, "/RecurringBill", UserId, body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        await _factory.RecurringBillRepository.Received(1).Create(Arg.Is<RecurringBill>(r =>
            r.UserId == UserId &&
            r.Name == "Rent" &&
            r.DueDay == 31 &&
            r.AmountType == AmountType.Fixed &&
            r.FixedValue == 1500m &&
            r.StartMonth == CurrentMonth &&
            r.EndMonth == null));
        var json = await ReadJsonAsync(response);
        json.GetProperty("id").GetInt64().Should().Be(3);
        json.GetProperty("startMonth").GetString().Should().Be("2026-10-01");
    }

    [Fact]
    public async Task Create_WithStartMonth_StartsInThatMonth()
    {
        // Arrange
        var body = new { name = "Power", dueDay = 5, amountType = "VariableEstimated", startMonth = "2026-01-15" };

        // Act
        var response = await SendAsync(HttpMethod.Post, "/RecurringBill", UserId, body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        await _factory.RecurringBillRepository.Received(1).Create(Arg.Is<RecurringBill>(r => r.StartMonth == new DateOnly(2026, 1, 1)));
    }

    [Theory]
    [MemberData(nameof(InvalidBodies))]
    public async Task Create_WithInvalidBody_ReturnsValidationProblem(object body)
    {
        // Act
        var response = await SendAsync(HttpMethod.Post, "/RecurringBill", UserId, body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
        await _factory.RecurringBillRepository.DidNotReceive().Create(Arg.Any<RecurringBill>());
    }

    public static TheoryData<object> InvalidBodies() => new()
    {
        new { name = "Rent", dueDay = 10, amountType = "Fixed" },
        new { name = "Power", dueDay = 10, amountType = "VariableEstimated", fixedValue = 100m },
        new { name = "Power", dueDay = 10, amountType = "VariableOnArrival", fixedValue = 100m },
        new { name = "Rent", dueDay = 0, amountType = "VariableOnArrival" },
        new { name = "Rent", dueDay = 32, amountType = "VariableOnArrival" },
        new { name = "", dueDay = 10, amountType = "VariableOnArrival" },
        new { name = "Rent", dueDay = 10 },
        new { name = "Rent", dueDay = 10, amountType = "Unknown" },
        new { name = "Rent", dueDay = 10, amountType = 0, fixedValue = 1m }
    };

    [Fact]
    public async Task Update_WithActiveRecurringBill_ReturnsUpdatedAndUpdatesUnpaidBillsFromCurrentMonth()
    {
        // Arrange
        _factory.RecurringBillRepository.GetById(3, UserId).Returns(Stored(UserId));
        var body = new { name = "Power", dueDay = 31, amountType = "VariableOnArrival" };

        // Act
        var response = await SendAsync(HttpMethod.Put, "/RecurringBill/3", UserId, body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ReadJsonAsync(response);
        json.GetProperty("name").GetString().Should().Be("Power");
        json.GetProperty("amountType").GetString().Should().Be("VariableOnArrival");
        json.GetProperty("fixedValue").ValueKind.Should().Be(JsonValueKind.Null);
        await _factory.BillRepository.Received(1).UpdateUnpaidFrom(
            Arg.Is<RecurringBill>(r => r.Id == 3 && r.UserId == UserId && r.Name == "Power" && r.DueDay == 31 && r.FixedValue == null),
            CurrentMonth);
    }

    [Fact]
    public async Task Update_WithOtherUsersRecurringBill_ReturnsNotFoundProblem()
    {
        // Arrange
        _factory.RecurringBillRepository.GetById(3, OtherUserId).Returns(Stored(OtherUserId));
        _factory.RecurringBillRepository.GetById(3, UserId).Returns((RecurringBill?)null);
        var body = new { name = "Power", dueDay = 31, amountType = "VariableOnArrival" };

        // Act
        var response = await SendAsync(HttpMethod.Put, "/RecurringBill/3", UserId, body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
        await _factory.RecurringBillRepository.DidNotReceive().Update(Arg.Any<RecurringBill>());
        await _factory.BillRepository.DidNotReceive().UpdateUnpaidFrom(Arg.Any<RecurringBill>(), Arg.Any<DateOnly>());
    }

    [Fact]
    public async Task Update_WithInvalidBody_ReturnsValidationProblem()
    {
        // Arrange
        var body = new { name = "Rent", dueDay = 10, amountType = "Fixed" };

        // Act
        var response = await SendAsync(HttpMethod.Put, "/RecurringBill/3", UserId, body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
    }

    [Fact]
    public async Task Update_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PutAsJsonAsync("/RecurringBill/3", new { name = "Rent", dueDay = 10, amountType = "VariableOnArrival" },
                                                    TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_WithActiveRecurringBill_ReturnsNoContentAndEndsInCurrentMonth()
    {
        // Arrange
        _factory.RecurringBillRepository.GetById(3, UserId).Returns(Stored(UserId));

        // Act
        var response = await SendAsync(HttpMethod.Delete, "/RecurringBill/3", UserId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await _factory.RecurringBillRepository.Received(1).Update(Arg.Is<RecurringBill>(r => r.Id == 3 && r.EndMonth == CurrentMonth));
        await _factory.BillRepository.Received(1).DeleteUnpaidFrom(3, CurrentMonth, UserId);
    }

    [Fact]
    public async Task Delete_WithOtherUsersRecurringBill_ReturnsNotFoundProblem()
    {
        // Arrange
        _factory.RecurringBillRepository.GetById(3, OtherUserId).Returns(Stored(OtherUserId));
        _factory.RecurringBillRepository.GetById(3, UserId).Returns((RecurringBill?)null);

        // Act
        var response = await SendAsync(HttpMethod.Delete, "/RecurringBill/3", UserId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
        await _factory.BillRepository.DidNotReceive().DeleteUnpaidFrom(Arg.Any<long>(), Arg.Any<DateOnly>(), Arg.Any<long>());
    }

    [Fact]
    public async Task Delete_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.DeleteAsync("/RecurringBill/3", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, long userId, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.For(_factory, new User("bruno") { Id = userId }));
        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
    }

    private static RecurringBill Stored(long userId)
    {
        return new RecurringBill(userId, "Rent", 10, AmountType.Fixed, new DateOnly(2026, 1, 1))
        {
            Id = 3,
            FixedValue = 1500m
        };
    }
}
