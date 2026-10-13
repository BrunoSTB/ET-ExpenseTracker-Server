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

    private readonly IBillRepository _billRepository = Substitute.For<IBillRepository>();
    private readonly IRecurringBillRepository _recurringBillRepository = Substitute.For<IRecurringBillRepository>();
    private readonly BillService _service;
    private List<Bill> _created = [];

    public BillServiceTests()
    {
        _billRepository.CreateMissing(Arg.Do<IEnumerable<Bill>>(bills => _created = bills.ToList())).Returns(0);
        _billRepository.GetByYear(Year, UserId).Returns([]);
        _recurringBillRepository.GetActive(UserId, new DateOnly(Year, 1, 1)).Returns([]);
        _service = new BillService(_billRepository, _recurringBillRepository);
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

    private void ActiveRecurringBills(params RecurringBill[] recurringBills)
    {
        _recurringBillRepository.GetActive(UserId, new DateOnly(Year, 1, 1)).Returns(recurringBills.ToList());
    }
}
