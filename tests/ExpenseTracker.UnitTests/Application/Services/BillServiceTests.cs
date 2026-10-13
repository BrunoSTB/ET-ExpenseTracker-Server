using ExpenseTracker.Application.Exceptions;
using ExpenseTracker.Application.IRepositories;
using ExpenseTracker.Application.Models;
using ExpenseTracker.Application.Services;
using ExpenseTracker.Domain.Models;
using NSubstitute;

namespace ExpenseTracker.UnitTests.Application.Services;

public class BillServiceTests
{
    private const long UserId = 7;
    private const int Year = 2026;
    private static readonly DateOnly Today = new(2026, 10, 13);

    private readonly IBillRepository _billRepository = Substitute.For<IBillRepository>();
    private readonly IRecurringBillRepository _recurringBillRepository = Substitute.For<IRecurringBillRepository>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly BillService _service;
    private List<Bill> _created = [];

    public BillServiceTests()
    {
        _billRepository.CreateMissing(Arg.Do<IEnumerable<Bill>>(bills => _created = bills.ToList())).Returns(0);
        _billRepository.GetByYear(Year, UserId).Returns([]);
        _recurringBillRepository.GetActive(UserId, new DateOnly(Year, 1, 1)).Returns([]);
        _billRepository.Update(Arg.Any<Bill>()).Returns(true);
        _billRepository.GetLastPaidValues(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<int>(), Arg.Any<long>()).Returns([]);
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero));
        _service = new BillService(_billRepository, _recurringBillRepository, _timeProvider);
    }

    [Fact]
    public async Task GetYear_WithOpenEndedRecurringBill_CreatesAllTwelveMonths()
    {
        // Arrange
        ActiveRecurringBills(new RecurringBill(UserId, "Rent", 31, AmountType.Fixed, new DateOnly(2025, 6, 1)) { Id = 3, FixedValue = 1500m });

        // Act
        await _service.GetYear(Year, UserId);

        // Assert
        _created.Select(b => b.DueDate).Should().Equal(
            new DateOnly(2026, 1, 31), new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 31), new DateOnly(2026, 4, 30),
            new DateOnly(2026, 5, 31), new DateOnly(2026, 6, 30), new DateOnly(2026, 7, 31), new DateOnly(2026, 8, 31),
            new DateOnly(2026, 9, 30), new DateOnly(2026, 10, 31), new DateOnly(2026, 11, 30), new DateOnly(2026, 12, 31));
        _created.Should().AllSatisfy(b =>
        {
            b.RecurringBillId.Should().Be(3);
            b.UserId.Should().Be(UserId);
            b.Value.Should().Be(1500m);
        });
    }

    [Fact]
    public async Task GetYear_WithStartAndEndMonthInYear_CreatesOnlyMonthsInWindow()
    {
        // Arrange
        ActiveRecurringBills(new RecurringBill(UserId, "Gym", 5, AmountType.VariableOnArrival, new DateOnly(2026, 3, 1))
        {
            Id = 4,
            EndMonth = new DateOnly(2026, 6, 1)
        });

        // Act
        await _service.GetYear(Year, UserId);

        // Assert
        _created.Select(b => b.DueDate.Month).Should().Equal(3, 4, 5);
        _created.Should().AllSatisfy(b => b.Value.Should().BeNull());
    }

    [Fact]
    public async Task GetYear_WithRecurringBillStartingNextYear_CreatesNothing()
    {
        // Arrange
        ActiveRecurringBills(new RecurringBill(UserId, "Rent", 10, AmountType.Fixed, new DateOnly(2027, 1, 1)) { Id = 3, FixedValue = 1m });

        // Act
        await _service.GetYear(Year, UserId);

        // Assert
        await _billRepository.DidNotReceive().CreateMissing(Arg.Any<IEnumerable<Bill>>());
    }

    [Fact]
    public async Task GetYear_WithVariableEstimatedBills_ReturnsAverageOfLastPaidValues()
    {
        // Arrange
        var bill = new Bill(UserId, "Power", AmountType.VariableEstimated, new DateOnly(2026, 5, 10)) { Id = 1, RecurringBillId = 4 };
        _billRepository.GetByYear(Year, UserId).Returns([bill]);
        _billRepository.GetLastPaidValues(Arg.Is<IReadOnlyCollection<long>>(ids => ids.SequenceEqual(new long[] { 4 })), BillService.EstimateSampleSize, UserId)
            .Returns(new Dictionary<long, List<decimal>> { [4] = [100m, 110m, 125m] });

        // Act
        var result = await _service.GetYear(Year, UserId);

        // Assert
        result.Should().ContainSingle().Which.Should().Be(new BillOverview(bill, 111.67m));
    }

    [Fact]
    public async Task GetYear_WithVariableEstimatedBillWithoutHistory_ReturnsNullEstimate()
    {
        // Arrange
        var bill = new Bill(UserId, "Power", AmountType.VariableEstimated, new DateOnly(2026, 5, 10)) { Id = 1, RecurringBillId = 4 };
        _billRepository.GetByYear(Year, UserId).Returns([bill]);
        _billRepository.GetLastPaidValues(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<int>(), UserId).Returns([]);

        // Act
        var result = await _service.GetYear(Year, UserId);

        // Assert
        result.Single().Estimate.Should().BeNull();
    }

    [Theory]
    [InlineData(AmountType.Fixed)]
    [InlineData(AmountType.VariableOnArrival)]
    public async Task GetYear_WithoutVariableEstimatedBills_ReturnsNoEstimateAndSkipsHistory(AmountType amountType)
    {
        // Arrange
        var bill = new Bill(UserId, "Rent", amountType, new DateOnly(2026, 5, 10)) { Id = 1, RecurringBillId = 4, Value = 10m };
        _billRepository.GetByYear(Year, UserId).Returns([bill]);

        // Act
        var result = await _service.GetYear(Year, UserId);

        // Assert
        result.Single().Estimate.Should().BeNull();
        await _billRepository.DidNotReceive().GetLastPaidValues(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<int>(), Arg.Any<long>());
    }

    [Fact]
    public async Task GetYear_WithStandaloneVariableEstimatedBill_ReturnsNullEstimate()
    {
        // Arrange
        var bill = new Bill(UserId, "One-off", AmountType.VariableEstimated, new DateOnly(2026, 5, 10)) { Id = 1 };
        _billRepository.GetByYear(Year, UserId).Returns([bill]);

        // Act
        var result = await _service.GetYear(Year, UserId);

        // Assert
        result.Single().Estimate.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithStandaloneBill_CreatesUnpaidBillWithoutRecurringBill()
    {
        // Arrange
        var bill = new Bill(UserId, "Gift", AmountType.Fixed, new DateOnly(2026, 12, 24))
        {
            RecurringBillId = 4,
            Value = 50m,
            PaidAt = Today
        };
        _billRepository.Create(Arg.Any<Bill>()).Returns(call => call.Arg<Bill>());

        // Act
        var result = await _service.Create(bill);

        // Assert
        await _billRepository.Received(1).Create(Arg.Is<Bill>(b => b.RecurringBillId == null && b.PaidAt == null && b.Value == 50m));
        result.Estimate.Should().BeNull();
    }

    [Fact]
    public async Task Delete_WithStandaloneBill_DeletesIt()
    {
        // Arrange
        _billRepository.GetById(1, UserId).Returns(Stored(AmountType.Fixed, recurringBillId: null));
        _billRepository.Delete(1, UserId).Returns(true);

        // Act
        var result = await _service.Delete(1, UserId);

        // Assert
        result.Should().BeTrue();
        await _billRepository.Received(1).Delete(1, UserId);
    }

    [Fact]
    public async Task Delete_WithRecurringBillBill_ThrowsRecurringBillOwnedException()
    {
        // Arrange
        _billRepository.GetById(1, UserId).Returns(Stored(AmountType.Fixed, recurringBillId: 4));

        // Act
        var act = () => _service.Delete(1, UserId);

        // Assert
        await act.Should().ThrowAsync<RecurringBillOwnedException>();
        await _billRepository.DidNotReceive().Delete(Arg.Any<long>(), Arg.Any<long>());
    }

    [Fact]
    public async Task Delete_WithMissingBill_ReturnsFalse()
    {
        // Act
        var result = await _service.Delete(1, UserId);

        // Assert
        result.Should().BeFalse();
        await _billRepository.DidNotReceive().Delete(Arg.Any<long>(), Arg.Any<long>());
    }

    [Fact]
    public async Task Pay_WithFixedBillWithoutValue_KeepsValueAndSetsPaidAtToToday()
    {
        // Arrange
        _billRepository.GetById(1, UserId).Returns(Stored(AmountType.Fixed, value: 1500m));

        // Act
        var result = await _service.Pay(1, UserId, null);

        // Assert
        result!.Bill.PaidAt.Should().Be(Today);
        result.Bill.Value.Should().Be(1500m);
        await _billRepository.Received(1).Update(Arg.Is<Bill>(b => b.Id == 1 && b.PaidAt == Today && b.Value == 1500m));
    }

    [Fact]
    public async Task Pay_WithFixedBillAndValue_OverwritesValue()
    {
        // Arrange
        _billRepository.GetById(1, UserId).Returns(Stored(AmountType.Fixed, value: 1500m));

        // Act
        var result = await _service.Pay(1, UserId, 1550m);

        // Assert
        result!.Bill.Value.Should().Be(1550m);
        await _billRepository.Received(1).Update(Arg.Is<Bill>(b => b.Value == 1550m && b.PaidAt == Today));
    }

    [Theory]
    [InlineData(AmountType.VariableEstimated)]
    [InlineData(AmountType.VariableOnArrival)]
    public async Task Pay_WithVariableBillWithoutValue_ThrowsBillValueRequiredException(AmountType amountType)
    {
        // Arrange
        _billRepository.GetById(1, UserId).Returns(Stored(amountType));

        // Act
        var act = () => _service.Pay(1, UserId, null);

        // Assert
        await act.Should().ThrowAsync<BillValueRequiredException>();
        await _billRepository.DidNotReceive().Update(Arg.Any<Bill>());
    }

    [Theory]
    [InlineData(AmountType.VariableEstimated)]
    [InlineData(AmountType.VariableOnArrival)]
    public async Task Pay_WithVariableBillAndValue_StoresValue(AmountType amountType)
    {
        // Arrange
        _billRepository.GetById(1, UserId).Returns(Stored(amountType));

        // Act
        var result = await _service.Pay(1, UserId, 230.5m);

        // Assert
        result!.Bill.Value.Should().Be(230.5m);
        result.Bill.PaidAt.Should().Be(Today);
    }

    [Fact]
    public async Task Pay_WithFutureBill_SetsPaidAtToToday()
    {
        // Arrange
        var bill = new Bill(UserId, "Rent", AmountType.Fixed, new DateOnly(2027, 3, 10)) { Id = 1, RecurringBillId = 4, Value = 1500m };
        _billRepository.GetById(1, UserId).Returns(bill);

        // Act
        var result = await _service.Pay(1, UserId, null);

        // Assert
        result!.Bill.PaidAt.Should().Be(Today);
    }

    [Fact]
    public async Task Pay_WithMissingBill_ReturnsNull()
    {
        // Act
        var result = await _service.Pay(1, UserId, 10m);

        // Assert
        result.Should().BeNull();
        await _billRepository.DidNotReceive().Update(Arg.Any<Bill>());
    }

    [Fact]
    public async Task Pay_WhenBillVanishesBeforeUpdate_ReturnsNull()
    {
        // Arrange
        _billRepository.GetById(1, UserId).Returns(Stored(AmountType.Fixed, value: 1500m));
        _billRepository.Update(Arg.Any<Bill>()).Returns(false);

        // Act
        var result = await _service.Pay(1, UserId, null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Pay_WithVariableEstimatedBill_ReturnsEstimate()
    {
        // Arrange
        _billRepository.GetById(1, UserId).Returns(Stored(AmountType.VariableEstimated));
        _billRepository.GetLastPaidValues(Arg.Any<IReadOnlyCollection<long>>(), BillService.EstimateSampleSize, UserId)
            .Returns(new Dictionary<long, List<decimal>> { [4] = [100m, 200m] });

        // Act
        var result = await _service.Pay(1, UserId, 120m);

        // Assert
        result!.Estimate.Should().Be(150m);
    }

    [Fact]
    public async Task Unpay_WithFixedBill_ClearsPaidAtAndKeepsValue()
    {
        // Arrange
        _billRepository.GetById(1, UserId).Returns(Stored(AmountType.Fixed, value: 1500m, paidAt: Today));

        // Act
        var result = await _service.Unpay(1, UserId);

        // Assert
        result!.Bill.PaidAt.Should().BeNull();
        result.Bill.Value.Should().Be(1500m);
        await _billRepository.Received(1).Update(Arg.Is<Bill>(b => b.PaidAt == null && b.Value == 1500m));
    }

    [Theory]
    [InlineData(AmountType.VariableEstimated)]
    [InlineData(AmountType.VariableOnArrival)]
    public async Task Unpay_WithVariableBill_ClearsPaidAtAndValue(AmountType amountType)
    {
        // Arrange
        _billRepository.GetById(1, UserId).Returns(Stored(amountType, value: 230m, paidAt: Today));

        // Act
        var result = await _service.Unpay(1, UserId);

        // Assert
        result!.Bill.PaidAt.Should().BeNull();
        result.Bill.Value.Should().BeNull();
        await _billRepository.Received(1).Update(Arg.Is<Bill>(b => b.PaidAt == null && b.Value == null));
    }

    [Fact]
    public async Task Unpay_WithMissingBill_ReturnsNull()
    {
        // Act
        var result = await _service.Unpay(1, UserId);

        // Assert
        result.Should().BeNull();
        await _billRepository.DidNotReceive().Update(Arg.Any<Bill>());
    }

    private static Bill Stored(AmountType amountType, decimal? value = null, DateOnly? paidAt = null, long? recurringBillId = 4)
    {
        return new Bill(UserId, "Bill", amountType, new DateOnly(2026, 10, 10))
        {
            Id = 1,
            RecurringBillId = recurringBillId,
            Value = value,
            PaidAt = paidAt
        };
    }

    private void ActiveRecurringBills(params RecurringBill[] recurringBills)
    {
        _recurringBillRepository.GetActive(UserId, new DateOnly(Year, 1, 1)).Returns(recurringBills.ToList());
    }
}
