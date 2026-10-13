using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExpenseTracker.Domain.Models;
using ExpenseTracker.IntegrationTests.Infrastructure;
using NSubstitute;

namespace ExpenseTracker.IntegrationTests.Api;

public class BillEndpointsTests : IAsyncDisposable
{
    private const long UserId = 7;
    private const string ProblemJson = "application/problem+json";

    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public BillEndpointsTests()
    {
        _client = _factory.CreateApiClient();
        _factory.RecurringBillRepository.GetActive(Arg.Any<long>(), Arg.Any<DateOnly>()).Returns([]);
        _factory.BillRepository.GetByYear(Arg.Any<int>(), Arg.Any<long>()).Returns([]);
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetYear_WithoutToken_ReturnsUnauthorizedProblem()
    {
        // Act
        var response = await _client.GetAsync("/Bill?year=2026", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
    }

    [Theory]
    [InlineData("/Bill")]
    [InlineData("/Bill?year=abc")]
    [InlineData("/Bill?year=1999")]
    [InlineData("/Bill?year=3000")]
    public async Task GetYear_WithInvalidYear_ReturnsValidationProblem(string url)
    {
        // Act
        var response = await SendAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
        await _factory.BillRepository.DidNotReceive().GetByYear(Arg.Any<int>(), Arg.Any<long>());
    }

    [Fact]
    public async Task GetYear_WithActiveRecurringBills_CreatesMissingBillsForCurrentUser()
    {
        // Arrange
        _factory.RecurringBillRepository.GetActive(UserId, new DateOnly(2026, 1, 1)).Returns(
        [
            new RecurringBill(UserId, "Rent", 31, AmountType.Fixed, new DateOnly(2026, 11, 1)) { Id = 3, FixedValue = 1500m }
        ]);

        // Act
        var response = await SendAsync("/Bill?year=2026");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await _factory.BillRepository.Received(1).CreateMissing(Arg.Is<IEnumerable<Bill>>(bills =>
            bills.Select(b => b.DueDate).SequenceEqual(new[] { new DateOnly(2026, 11, 30), new DateOnly(2026, 12, 31) }) &&
            bills.All(b => b.UserId == UserId && b.RecurringBillId == 3 && b.Value == 1500m)));
        await _factory.BillRepository.Received(1).GetByYear(2026, UserId);
    }

    [Fact]
    public async Task GetYear_WithBills_ReturnsTwelveMonthsWithTheirBills()
    {
        // Arrange
        _factory.BillRepository.GetByYear(2026, UserId).Returns(
        [
            new Bill(UserId, "Power", AmountType.VariableEstimated, new DateOnly(2026, 2, 10)) { Id = 11, RecurringBillId = 4 },
            new Bill(UserId, "Rent", AmountType.Fixed, new DateOnly(2026, 2, 28))
            {
                Id = 12,
                RecurringBillId = 3,
                Value = 1500m,
                PaidAt = new DateOnly(2026, 2, 20)
            },
            new Bill(UserId, "Gift", AmountType.VariableOnArrival, new DateOnly(2026, 12, 24)) { Id = 13 }
        ]);
        _factory.BillRepository.GetLastPaidValues(Arg.Any<IReadOnlyCollection<long>>(), 3, UserId)
            .Returns(new Dictionary<long, List<decimal>> { [4] = [100m, 200m] });

        // Act
        var response = await SendAsync("/Bill?year=2026");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        json.EnumerateArray().Select(m => m.GetProperty("month").GetInt32()).Should().Equal(Enumerable.Range(1, 12));
        json.EnumerateArray().Select(m => m.GetProperty("bills").GetArrayLength()).Should().Equal(0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);

        var power = json[1].GetProperty("bills")[0];
        power.GetProperty("id").GetInt64().Should().Be(11);
        power.GetProperty("recurringBillId").GetInt64().Should().Be(4);
        power.GetProperty("name").GetString().Should().Be("Power");
        power.GetProperty("dueDate").GetString().Should().Be("2026-02-10");
        power.GetProperty("amountType").GetString().Should().Be("VariableEstimated");
        power.GetProperty("value").ValueKind.Should().Be(JsonValueKind.Null);
        power.GetProperty("estimate").GetDecimal().Should().Be(150m);
        power.GetProperty("paidAt").ValueKind.Should().Be(JsonValueKind.Null);

        var rent = json[1].GetProperty("bills")[1];
        rent.GetProperty("value").GetDecimal().Should().Be(1500m);
        rent.GetProperty("estimate").ValueKind.Should().Be(JsonValueKind.Null);
        rent.GetProperty("paidAt").GetString().Should().Be("2026-02-20");

        json[11].GetProperty("bills")[0].GetProperty("recurringBillId").ValueKind.Should().Be(JsonValueKind.Null);
    }

    private async Task<HttpResponseMessage> SendAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.For(_factory, new User("bruno") { Id = UserId }));
        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
