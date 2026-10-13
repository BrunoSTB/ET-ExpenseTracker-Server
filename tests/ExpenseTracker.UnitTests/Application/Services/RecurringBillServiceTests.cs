using ExpenseTracker.Application.IRepositories;
using ExpenseTracker.Application.Services;
using ExpenseTracker.Domain.Models;
using NSubstitute;

namespace ExpenseTracker.UnitTests.Application.Services;

public class RecurringBillServiceTests
{
    private const long UserId = 7;
    private static readonly DateOnly CurrentMonth = new(2026, 10, 1);

    private readonly IRecurringBillRepository _recurringBillRepository = Substitute.For<IRecurringBillRepository>();
    private readonly IBillRepository _billRepository = Substitute.For<IBillRepository>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly RecurringBillService _service;

    public RecurringBillServiceTests()
    {
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero));
        _service = new RecurringBillService(_recurringBillRepository, _billRepository, _timeProvider);
    }

    [Fact]
    public async Task GetActive_WithUser_QueriesFromCurrentMonth()
    {
        // Arrange
        var active = new List<RecurringBill> { Stored() };
        _recurringBillRepository.GetActive(UserId, CurrentMonth).Returns(active);

        // Act
        var result = await _service.GetActive(UserId);

        // Assert
        result.Should().BeSameAs(active);
    }

    [Fact]
    public async Task Create_WithoutStartMonth_StartsInCurrentMonth()
    {
        // Arrange
        _recurringBillRepository.Create(Arg.Any<RecurringBill>()).Returns(call => call.Arg<RecurringBill>());
        var recurringBill = new RecurringBill(UserId, "Rent", 10, AmountType.Fixed, default) { FixedValue = 1500m };

        // Act
        var result = await _service.Create(recurringBill);

        // Assert
        result.StartMonth.Should().Be(CurrentMonth);
        result.EndMonth.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithStartMonth_NormalizesToFirstDayOfMonth()
    {
        // Arrange
        _recurringBillRepository.Create(Arg.Any<RecurringBill>()).Returns(call => call.Arg<RecurringBill>());
        var recurringBill = new RecurringBill(UserId, "Rent", 10, AmountType.Fixed, new DateOnly(2026, 1, 20));

        // Act
        var result = await _service.Create(recurringBill);

        // Assert
        result.StartMonth.Should().Be(new DateOnly(2026, 1, 1));
    }

    [Fact]
    public async Task Update_WithActiveRecurringBill_UpdatesModelAndUnpaidBillsFromCurrentMonth()
    {
        // Arrange
        var stored = Stored();
        _recurringBillRepository.GetById(stored.Id, UserId).Returns(stored);
        var edited = new RecurringBill(UserId, "Power", 31, AmountType.VariableEstimated, default) { Id = stored.Id };

        // Act
        var result = await _service.Update(edited);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Power");
        result.DueDay.Should().Be(31);
        result.AmountType.Should().Be(AmountType.VariableEstimated);
        result.FixedValue.Should().BeNull();
        result.StartMonth.Should().Be(stored.StartMonth);
        await _recurringBillRepository.Received(1).Update(result);
        await _billRepository.Received(1).UpdateUnpaidFrom(result, CurrentMonth);
    }

    [Fact]
    public async Task Update_WithMissingRecurringBill_ReturnsNullAndChangesNothing()
    {
        // Arrange
        _recurringBillRepository.GetById(99, UserId).Returns((RecurringBill?)null);
        var edited = new RecurringBill(UserId, "Power", 10, AmountType.VariableEstimated, default) { Id = 99 };

        // Act
        var result = await _service.Update(edited);

        // Assert
        result.Should().BeNull();
        await _recurringBillRepository.DidNotReceive().Update(Arg.Any<RecurringBill>());
        await _billRepository.DidNotReceive().UpdateUnpaidFrom(Arg.Any<RecurringBill>(), Arg.Any<DateOnly>());
    }

    [Fact]
    public async Task Update_WithEndedRecurringBill_ReturnsNull()
    {
        // Arrange
        var stored = Stored();
        stored.EndMonth = CurrentMonth;
        _recurringBillRepository.GetById(stored.Id, UserId).Returns(stored);

        // Act
        var result = await _service.Update(new RecurringBill(UserId, "Power", 10, AmountType.VariableEstimated, default) { Id = stored.Id });

        // Assert
        result.Should().BeNull();
        await _billRepository.DidNotReceive().UpdateUnpaidFrom(Arg.Any<RecurringBill>(), Arg.Any<DateOnly>());
    }

    [Fact]
    public async Task Delete_WithActiveRecurringBill_EndsInCurrentMonthAndDeletesUnpaidBills()
    {
        // Arrange
        var stored = Stored();
        _recurringBillRepository.GetById(stored.Id, UserId).Returns(stored);

        // Act
        var result = await _service.Delete(stored.Id, UserId);

        // Assert
        result.Should().BeTrue();
        await _recurringBillRepository.Received(1).Update(Arg.Is<RecurringBill>(r => r.Id == stored.Id && r.EndMonth == CurrentMonth));
        await _billRepository.Received(1).DeleteUnpaidFrom(stored.Id, CurrentMonth, UserId);
    }

    [Fact]
    public async Task Delete_WithMissingRecurringBill_ReturnsFalseAndChangesNothing()
    {
        // Arrange
        _recurringBillRepository.GetById(99, UserId).Returns((RecurringBill?)null);

        // Act
        var result = await _service.Delete(99, UserId);

        // Assert
        result.Should().BeFalse();
        await _recurringBillRepository.DidNotReceive().Update(Arg.Any<RecurringBill>());
        await _billRepository.DidNotReceive().DeleteUnpaidFrom(Arg.Any<long>(), Arg.Any<DateOnly>(), Arg.Any<long>());
    }

    [Fact]
    public async Task Delete_WithAlreadyEndedRecurringBill_ReturnsFalse()
    {
        // Arrange
        var stored = Stored();
        stored.EndMonth = new DateOnly(2026, 5, 1);
        _recurringBillRepository.GetById(stored.Id, UserId).Returns(stored);

        // Act
        var result = await _service.Delete(stored.Id, UserId);

        // Assert
        result.Should().BeFalse();
        await _billRepository.DidNotReceive().DeleteUnpaidFrom(Arg.Any<long>(), Arg.Any<DateOnly>(), Arg.Any<long>());
    }

    private static RecurringBill Stored()
    {
        return new RecurringBill(UserId, "Rent", 10, AmountType.Fixed, new DateOnly(2026, 1, 1))
        {
            Id = 3,
            FixedValue = 1500m
        };
    }
}
