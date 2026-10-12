using ExpenseTracker.Domain.Models;
using ExpenseTracker.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExpenseTracker.IntegrationTests.Infrastructure.Repositories;

public class BillRepositoryTests
{
    private readonly PostgresFixture _postgres;

    public BillRepositoryTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task Create_WithSameRecurringBillInSameMonth_ThrowsUniqueViolation()
    {
        // Arrange
        var userId = await _postgres.CreateUserAsync();
        var recurringBillId = await CreateRecurringBillAsync(userId);
        await CreateAsync(new Bill(userId, "Rent", AmountType.Fixed, new DateOnly(2026, 3, 5)) { RecurringBillId = recurringBillId });

        // Act
        var act = () => CreateAsync(new Bill(userId, "Rent", AmountType.Fixed, new DateOnly(2026, 3, 20)) { RecurringBillId = recurringBillId });

        // Assert
        (await act.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public async Task Create_WithStandaloneBillsInSameMonth_CreatesBoth()
    {
        // Arrange
        var userId = await _postgres.CreateUserAsync();

        // Act
        await CreateAsync(new Bill(userId, "Gift", AmountType.VariableOnArrival, new DateOnly(2026, 3, 5)));
        await CreateAsync(new Bill(userId, "Repair", AmountType.VariableOnArrival, new DateOnly(2026, 3, 5)));

        // Assert
        var bills = await GetByYearAsync(2026, userId);
        bills.Should().HaveCount(2);
    }

    [Fact]
    public async Task Create_WithUnknownRecurringBill_ThrowsForeignKeyViolation()
    {
        // Arrange
        var userId = await _postgres.CreateUserAsync();
        var bill = new Bill(userId, "Rent", AmountType.Fixed, new DateOnly(2026, 3, 5)) { RecurringBillId = long.MaxValue };

        // Act
        var act = () => CreateAsync(bill);

        // Assert
        (await act.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
    }

    [Fact]
    public async Task Create_WithAllFields_RoundTripsThroughGetById()
    {
        // Arrange
        var userId = await _postgres.CreateUserAsync();
        var recurringBillId = await CreateRecurringBillAsync(userId);
        var bill = new Bill(userId, "Power", AmountType.VariableEstimated, new DateOnly(2026, 2, 28))
        {
            RecurringBillId = recurringBillId,
            Value = 123.45m,
            PaidAt = new DateOnly(2026, 2, 27)
        };

        // Act
        var created = await CreateAsync(bill);
        var result = await GetByIdAsync(created.Id, userId);

        // Assert
        result.Should().BeEquivalentTo(bill, options => options.Excluding(b => b.Id));
        result!.Id.Should().Be(created.Id).And.BePositive();
    }

    [Fact]
    public async Task CreateMissing_WhenRunTwice_CreatesEachMonthOnce()
    {
        // Arrange
        var userId = await _postgres.CreateUserAsync();
        var recurringBillId = await CreateRecurringBillAsync(userId);
        var bills = Enumerable.Range(1, 12)
            .Select(month => new Bill(userId, "Rent", AmountType.Fixed, new DateOnly(2026, month, 10))
            {
                RecurringBillId = recurringBillId,
                Value = 1500m
            })
            .ToList();

        // Act
        await using (var context = _postgres.CreateContext())
        {
            (await new BillRepository(context).CreateMissing(bills)).Should().Be(12);
        }
        await using (var context = _postgres.CreateContext())
        {
            (await new BillRepository(context).CreateMissing(bills)).Should().Be(0);
        }

        // Assert
        var result = await GetByYearAsync(2026, userId);
        result.Should().HaveCount(12);
        result.Select(b => b.DueDate.Month).Should().BeEquivalentTo(Enumerable.Range(1, 12));
        result.Should().OnlyContain(b => b.RecurringBillId == recurringBillId && b.Value == 1500m && b.PaidAt == null);
    }

    [Fact]
    public async Task GetByYear_WithBillsOfOtherUsersAndYears_ReturnsOnlyUserBillsOfYear()
    {
        // Arrange
        var userId = await _postgres.CreateUserAsync();
        var otherUserId = await _postgres.CreateUserAsync();
        await CreateAsync(new Bill(userId, "December", AmountType.Fixed, new DateOnly(2025, 12, 31)));
        await CreateAsync(new Bill(userId, "February", AmountType.Fixed, new DateOnly(2026, 2, 1)));
        await CreateAsync(new Bill(userId, "January", AmountType.Fixed, new DateOnly(2026, 1, 15)));
        await CreateAsync(new Bill(userId, "Next year", AmountType.Fixed, new DateOnly(2027, 1, 1)));
        await CreateAsync(new Bill(otherUserId, "Other user", AmountType.Fixed, new DateOnly(2026, 1, 15)));

        // Act
        var result = await GetByYearAsync(2026, userId);

        // Assert
        result.Select(b => b.Name).Should().Equal("January", "February");
    }

    [Fact]
    public async Task GetById_WithBillOfOtherUser_ReturnsNull()
    {
        // Arrange
        var userId = await _postgres.CreateUserAsync();
        var otherUserId = await _postgres.CreateUserAsync();
        var bill = await CreateAsync(new Bill(userId, "Rent", AmountType.Fixed, new DateOnly(2026, 3, 5)));

        // Act
        var result = await GetByIdAsync(bill.Id, otherUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Update_WithBillOfUser_PersistsChanges()
    {
        // Arrange
        var userId = await _postgres.CreateUserAsync();
        var bill = await CreateAsync(new Bill(userId, "Water", AmountType.VariableOnArrival, new DateOnly(2026, 3, 5)));
        bill.Value = 80m;
        bill.PaidAt = new DateOnly(2026, 3, 4);

        // Act
        bool updated;
        await using (var context = _postgres.CreateContext())
        {
            updated = await new BillRepository(context).Update(bill);
        }

        // Assert
        updated.Should().BeTrue();
        var result = await GetByIdAsync(bill.Id, userId);
        result!.Value.Should().Be(80m);
        result.PaidAt.Should().Be(new DateOnly(2026, 3, 4));
    }

    [Fact]
    public async Task Update_WithBillOfOtherUser_ReturnsFalseAndKeepsBill()
    {
        // Arrange
        var userId = await _postgres.CreateUserAsync();
        var otherUserId = await _postgres.CreateUserAsync();
        var bill = await CreateAsync(new Bill(userId, "Water", AmountType.VariableOnArrival, new DateOnly(2026, 3, 5)));
        var tampered = new Bill(otherUserId, "Tampered", AmountType.Fixed, bill.DueDate) { Id = bill.Id, Value = 1m };

        // Act
        bool updated;
        await using (var context = _postgres.CreateContext())
        {
            updated = await new BillRepository(context).Update(tampered);
        }

        // Assert
        updated.Should().BeFalse();
        var result = await GetByIdAsync(bill.Id, userId);
        result!.Name.Should().Be("Water");
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Delete_WithBillOfOtherUser_ReturnsFalseAndKeepsBill()
    {
        // Arrange
        var userId = await _postgres.CreateUserAsync();
        var otherUserId = await _postgres.CreateUserAsync();
        var bill = await CreateAsync(new Bill(userId, "Rent", AmountType.Fixed, new DateOnly(2026, 3, 5)));

        // Act
        bool deleted;
        await using (var context = _postgres.CreateContext())
        {
            deleted = await new BillRepository(context).Delete(bill.Id, otherUserId);
        }

        // Assert
        deleted.Should().BeFalse();
        (await GetByIdAsync(bill.Id, userId)).Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_WithBillOfUser_ReturnsTrueAndRemovesBill()
    {
        // Arrange
        var userId = await _postgres.CreateUserAsync();
        var bill = await CreateAsync(new Bill(userId, "Rent", AmountType.Fixed, new DateOnly(2026, 3, 5)));

        // Act
        bool deleted;
        await using (var context = _postgres.CreateContext())
        {
            deleted = await new BillRepository(context).Delete(bill.Id, userId);
        }

        // Assert
        deleted.Should().BeTrue();
        (await GetByIdAsync(bill.Id, userId)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteUnpaidFrom_WithMixedBills_DeletesOnlyUnpaidBillsFromMonthOn()
    {
        // Arrange
        var userId = await _postgres.CreateUserAsync();
        var recurringBillId = await CreateRecurringBillAsync(userId);
        var otherRecurringBillId = await CreateRecurringBillAsync(userId);
        await CreateAsync(new Bill(userId, "Past", AmountType.Fixed, new DateOnly(2026, 2, 10)) { RecurringBillId = recurringBillId });
        await CreateAsync(new Bill(userId, "Current", AmountType.Fixed, new DateOnly(2026, 3, 10)) { RecurringBillId = recurringBillId });
        await CreateAsync(new Bill(userId, "Paid ahead", AmountType.Fixed, new DateOnly(2026, 4, 10))
        {
            RecurringBillId = recurringBillId,
            PaidAt = new DateOnly(2026, 3, 1)
        });
        await CreateAsync(new Bill(userId, "Future", AmountType.Fixed, new DateOnly(2026, 5, 10)) { RecurringBillId = recurringBillId });
        await CreateAsync(new Bill(userId, "Other model", AmountType.Fixed, new DateOnly(2026, 5, 10)) { RecurringBillId = otherRecurringBillId });

        // Act
        int deleted;
        await using (var context = _postgres.CreateContext())
        {
            deleted = await new BillRepository(context).DeleteUnpaidFrom(recurringBillId, new DateOnly(2026, 3, 25), userId);
        }

        // Assert
        deleted.Should().Be(2);
        var result = await GetByYearAsync(2026, userId);
        result.Select(b => b.Name).Should().Equal("Past", "Paid ahead", "Other model");
    }

    [Fact]
    public async Task DeleteUnpaidFrom_WithOtherUser_DeletesNothing()
    {
        // Arrange
        var userId = await _postgres.CreateUserAsync();
        var otherUserId = await _postgres.CreateUserAsync();
        var recurringBillId = await CreateRecurringBillAsync(userId);
        await CreateAsync(new Bill(userId, "Rent", AmountType.Fixed, new DateOnly(2026, 3, 10)) { RecurringBillId = recurringBillId });

        // Act
        int deleted;
        await using (var context = _postgres.CreateContext())
        {
            deleted = await new BillRepository(context).DeleteUnpaidFrom(recurringBillId, new DateOnly(2026, 1, 1), otherUserId);
        }

        // Assert
        deleted.Should().Be(0);
        (await GetByYearAsync(2026, userId)).Should().ContainSingle();
    }

    private async Task<long> CreateRecurringBillAsync(long userId)
    {
        await using var context = _postgres.CreateContext();
        var recurringBill = new RecurringBill(userId, "Rent", 10, AmountType.Fixed, new DateOnly(2026, 1, 1)) { FixedValue = 1500m };
        return (await new RecurringBillRepository(context).Create(recurringBill)).Id;
    }

    private async Task<Bill> CreateAsync(Bill bill)
    {
        await using var context = _postgres.CreateContext();
        return await new BillRepository(context).Create(bill);
    }

    private async Task<Bill?> GetByIdAsync(long id, long userId)
    {
        await using var context = _postgres.CreateContext();
        return await new BillRepository(context).GetById(id, userId);
    }

    private async Task<List<Bill>> GetByYearAsync(int year, long userId)
    {
        await using var context = _postgres.CreateContext();
        return await new BillRepository(context).GetByYear(year, userId);
    }
}
