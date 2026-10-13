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
    private const long OtherUserId = 8;
    private const string ProblemJson = "application/problem+json";

    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public BillEndpointsTests()
    {
        _client = _factory.CreateApiClient();
        _factory.RecurringBillRepository.GetActive(Arg.Any<long>(), Arg.Any<DateOnly>()).Returns([]);
        _factory.BillRepository.GetByYear(Arg.Any<int>(), Arg.Any<long>()).Returns([]);
        _factory.BillRepository.Update(Arg.Any<Bill>()).Returns(true);
        _factory.BillRepository.GetLastPaidValues(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<int>(), Arg.Any<long>()).Returns([]);
        _factory.BillRepository.Create(Arg.Any<Bill>())
            .Returns(call =>
            {
                var bill = call.Arg<Bill>();
                bill.Id = 20;
                return bill;
            });
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
        var response = await SendAsync(HttpMethod.Get, url);

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
        var response = await SendAsync(HttpMethod.Get, "/Bill?year=2026");

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
        var response = await SendAsync(HttpMethod.Get, "/Bill?year=2026");

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

    [Fact]
    public async Task Create_WithValidBody_ReturnsCreatedStandaloneBillForCurrentUser()
    {
        // Arrange
        var body = new { name = "Gift", dueDate = "2026-12-24", amountType = "Fixed", value = 50m, userId = OtherUserId, recurringBillId = 3 };

        // Act
        var response = await SendAsync(HttpMethod.Post, "/Bill", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        await _factory.BillRepository.Received(1).Create(Arg.Is<Bill>(b =>
            b.UserId == UserId &&
            b.RecurringBillId == null &&
            b.Name == "Gift" &&
            b.DueDate == new DateOnly(2026, 12, 24) &&
            b.AmountType == AmountType.Fixed &&
            b.Value == 50m &&
            b.PaidAt == null));
        var json = await ReadJsonAsync(response);
        json.GetProperty("id").GetInt64().Should().Be(20);
        json.GetProperty("recurringBillId").ValueKind.Should().Be(JsonValueKind.Null);
        json.GetProperty("dueDate").GetString().Should().Be("2026-12-24");
        json.GetProperty("paidAt").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Theory]
    [MemberData(nameof(InvalidBills))]
    public async Task Create_WithInvalidBody_ReturnsValidationProblem(object body)
    {
        // Act
        var response = await SendAsync(HttpMethod.Post, "/Bill", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
        await _factory.BillRepository.DidNotReceive().Create(Arg.Any<Bill>());
    }

    public static TheoryData<object> InvalidBills() => new()
    {
        new { name = "Gift", dueDate = "2026-12-24", amountType = "Fixed" },
        new { name = "Gift", dueDate = "2026-12-24", amountType = "VariableOnArrival", value = 10m },
        new { name = "Gift", dueDate = "2026-12-24", amountType = "Fixed", value = -1m },
        new { name = "Gift", amountType = "VariableOnArrival" },
        new { name = "Gift", dueDate = "1999-12-24", amountType = "VariableOnArrival" },
        new { name = "Gift", dueDate = "not-a-date", amountType = "VariableOnArrival" },
        new { name = "", dueDate = "2026-12-24", amountType = "VariableOnArrival" },
        new { name = "Gift", dueDate = "2026-12-24" },
        new { name = "Gift", dueDate = "2026-12-24", amountType = 0, value = 1m }
    };

    [Fact]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/Bill", new { name = "Gift", dueDate = "2026-12-24", amountType = "VariableOnArrival" },
                                                     TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_WithStandaloneBill_ReturnsNoContent()
    {
        // Arrange
        _factory.BillRepository.GetById(20, UserId).Returns(Stored(AmountType.Fixed, value: 50m, recurringBillId: null));
        _factory.BillRepository.Delete(20, UserId).Returns(true);

        // Act
        var response = await SendAsync(HttpMethod.Delete, "/Bill/20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await _factory.BillRepository.Received(1).Delete(20, UserId);
    }

    [Fact]
    public async Task Delete_WithRecurringBillBill_SkipsItAndReturnsNoContent()
    {
        // Arrange
        _factory.BillRepository.GetById(20, UserId).Returns(Stored(AmountType.Fixed, value: 50m));
        _factory.BillRepository.Skip(20, new DateOnly(2026, 10, 13), UserId).Returns(true);

        // Act
        var response = await SendAsync(HttpMethod.Delete, "/Bill/20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await _factory.BillRepository.Received(1).Skip(20, new DateOnly(2026, 10, 13), UserId);
        await _factory.BillRepository.DidNotReceive().Delete(Arg.Any<long>(), Arg.Any<long>());
    }

    [Fact]
    public async Task Delete_WithAlreadySkippedBill_ReturnsNotFoundProblem()
    {
        // Act
        var response = await SendAsync(HttpMethod.Delete, "/Bill/20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
        await _factory.BillRepository.DidNotReceive().Skip(Arg.Any<long>(), Arg.Any<DateOnly>(), Arg.Any<long>());
    }

    [Fact]
    public async Task Delete_WithOtherUsersBill_ReturnsNotFoundProblem()
    {
        // Arrange
        _factory.BillRepository.GetById(20, OtherUserId).Returns(Stored(AmountType.Fixed, value: 50m, recurringBillId: null, userId: OtherUserId));

        // Act
        var response = await SendAsync(HttpMethod.Delete, "/Bill/20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
        await _factory.BillRepository.DidNotReceive().Delete(Arg.Any<long>(), Arg.Any<long>());
    }

    [Fact]
    public async Task Delete_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.DeleteAsync("/Bill/20", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Pay_WithFixedBillAndEmptyBody_ReturnsBillPaidTodayWithItsValue()
    {
        // Arrange
        _factory.BillRepository.GetById(20, UserId).Returns(Stored(AmountType.Fixed, value: 1500m));

        // Act
        var response = await SendAsync(HttpMethod.Put, "/Bill/20/Payment");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ReadJsonAsync(response);
        json.GetProperty("id").GetInt64().Should().Be(20);
        json.GetProperty("value").GetDecimal().Should().Be(1500m);
        json.GetProperty("paidAt").GetString().Should().Be("2026-10-13");
        await _factory.BillRepository.Received(1).Update(Arg.Is<Bill>(b =>
            b.Id == 20 && b.UserId == UserId && b.Value == 1500m && b.PaidAt == new DateOnly(2026, 10, 13)));
    }

    [Fact]
    public async Task Pay_WithFixedBillAndValue_OverwritesValue()
    {
        // Arrange
        _factory.BillRepository.GetById(20, UserId).Returns(Stored(AmountType.Fixed, value: 1500m));

        // Act
        var response = await SendAsync(HttpMethod.Put, "/Bill/20/Payment", new { value = 1550.25m });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ReadJsonAsync(response);
        json.GetProperty("value").GetDecimal().Should().Be(1550.25m);
    }

    [Theory]
    [InlineData("VariableEstimated")]
    [InlineData("VariableOnArrival")]
    public async Task Pay_WithVariableBillAndValue_ReturnsBillPaidWithValue(string amountType)
    {
        // Arrange
        _factory.BillRepository.GetById(20, UserId).Returns(Stored(Enum.Parse<AmountType>(amountType)));

        // Act
        var response = await SendAsync(HttpMethod.Put, "/Bill/20/Payment", new { value = 230.5m });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ReadJsonAsync(response);
        json.GetProperty("amountType").GetString().Should().Be(amountType);
        json.GetProperty("value").GetDecimal().Should().Be(230.5m);
        json.GetProperty("paidAt").GetString().Should().Be("2026-10-13");
    }

    [Theory]
    [InlineData(AmountType.VariableEstimated)]
    [InlineData(AmountType.VariableOnArrival)]
    public async Task Pay_WithVariableBillWithoutValue_ReturnsBadRequestProblem(AmountType amountType)
    {
        // Arrange
        _factory.BillRepository.GetById(20, UserId).Returns(Stored(amountType));

        // Act
        var response = await SendAsync(HttpMethod.Put, "/Bill/20/Payment", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
        await _factory.BillRepository.DidNotReceive().Update(Arg.Any<Bill>());
    }

    [Fact]
    public async Task Pay_WithNegativeValue_ReturnsValidationProblem()
    {
        // Act
        var response = await SendAsync(HttpMethod.Put, "/Bill/20/Payment", new { value = -1m });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
        await _factory.BillRepository.DidNotReceive().GetById(Arg.Any<long>(), Arg.Any<long>());
    }

    [Fact]
    public async Task Pay_WithOtherUsersBill_ReturnsNotFoundProblem()
    {
        // Arrange
        _factory.BillRepository.GetById(20, OtherUserId).Returns(Stored(AmountType.Fixed, value: 1500m, userId: OtherUserId));

        // Act
        var response = await SendAsync(HttpMethod.Put, "/Bill/20/Payment", new { value = 10m });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
        await _factory.BillRepository.DidNotReceive().Update(Arg.Any<Bill>());
    }

    [Fact]
    public async Task Pay_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PutAsJsonAsync("/Bill/20/Payment", new { value = 10m }, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unpay_WithVariableBill_ReturnsBillWithoutPaymentAndValue()
    {
        // Arrange
        _factory.BillRepository.GetById(20, UserId).Returns(Stored(AmountType.VariableEstimated, value: 230m, paidAt: new DateOnly(2026, 10, 1)));
        _factory.BillRepository.GetLastPaidValues(Arg.Any<IReadOnlyCollection<long>>(), 3, UserId)
            .Returns(new Dictionary<long, List<decimal>> { [3] = [100m, 200m] });

        // Act
        var response = await SendAsync(HttpMethod.Delete, "/Bill/20/Payment");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ReadJsonAsync(response);
        json.GetProperty("value").ValueKind.Should().Be(JsonValueKind.Null);
        json.GetProperty("paidAt").ValueKind.Should().Be(JsonValueKind.Null);
        json.GetProperty("estimate").GetDecimal().Should().Be(150m);
        await _factory.BillRepository.Received(1).Update(Arg.Is<Bill>(b => b.Id == 20 && b.PaidAt == null && b.Value == null));
    }

    [Fact]
    public async Task Unpay_WithFixedBill_KeepsValue()
    {
        // Arrange
        _factory.BillRepository.GetById(20, UserId).Returns(Stored(AmountType.Fixed, value: 1500m, paidAt: new DateOnly(2026, 10, 1)));

        // Act
        var response = await SendAsync(HttpMethod.Delete, "/Bill/20/Payment");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ReadJsonAsync(response);
        json.GetProperty("value").GetDecimal().Should().Be(1500m);
        json.GetProperty("paidAt").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Unpay_WithOtherUsersBill_ReturnsNotFoundProblem()
    {
        // Arrange
        _factory.BillRepository.GetById(20, OtherUserId).Returns(Stored(AmountType.Fixed, value: 1500m, userId: OtherUserId));

        // Act
        var response = await SendAsync(HttpMethod.Delete, "/Bill/20/Payment");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
        await _factory.BillRepository.DidNotReceive().Update(Arg.Any<Bill>());
    }

    [Fact]
    public async Task Unpay_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.DeleteAsync("/Bill/20/Payment", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.For(_factory, new User("bruno") { Id = UserId }));
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

    private static Bill Stored(AmountType amountType, decimal? value = null, DateOnly? paidAt = null, long? recurringBillId = 3, long userId = UserId)
    {
        return new Bill(userId, "Bill", amountType, new DateOnly(2026, 10, 10))
        {
            Id = 20,
            RecurringBillId = recurringBillId,
            Value = value,
            PaidAt = paidAt
        };
    }
}
