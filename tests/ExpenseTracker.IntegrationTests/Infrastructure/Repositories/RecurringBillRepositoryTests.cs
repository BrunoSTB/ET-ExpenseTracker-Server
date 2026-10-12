using ExpenseTracker.Domain.Models;
using ExpenseTracker.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExpenseTracker.IntegrationTests.Infrastructure.Repositories;

public class RecurringBillRepositoryTests
{
    private readonly PostgresFixture _postgres;

    public RecurringBillRepositoryTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task Create_WithAllFields_RoundTripsThroughGetById()
    {
        // Arrange
        var userId = await _postgres.CreateUserAsync();
        var recurringBill = new RecurringBill(userId, "Rent", 31, AmountType.Fixed, new DateOnly(2026, 1, 1))
        {
            FixedValue = 1500.50m,
            EndMonth = new DateOnly(2026, 12, 1)
        };

        // Act
        var created = await CreateAsync(recurringBill);
        var result = await GetByIdAsync(created.Id, userId);

        // Assert
        result.Should().BeEquivalentTo(recurringBill, options => options.Excluding(r => r.Id));
        result!.Id.Should().Be(created.Id).And.BePositive();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public async Task Create_WithDueDayOutOfRange_ThrowsCheckViolation(int dueDay)
    {
        // Arrange
        var userId = await _postgres.CreateUserAsync();
        var recurringBill = new RecurringBill(userId, "Rent", dueDay, AmountType.Fixed, new DateOnly(2026, 1, 1));

        // Act
        var act = () => CreateAsync(recurringBill);

        // Assert
        (await act.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
    }

    [Fact]
    public async Task GetById_WithRecurringBillOfOtherUser_ReturnsNull()
    {
        // Arrange
        var userId = await _postgres.CreateUserAsync();
        var otherUserId = await _postgres.CreateUserAsync();
        var created = await CreateAsync(new RecurringBill(userId, "Rent", 10, AmountType.Fixed, new DateOnly(2026, 1, 1)));

        // Act
        var result = await GetByIdAsync(created.Id, otherUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActive_WithEndedAndOtherUsersBills_ReturnsOnlyUserBillsActiveFromMonth()
    {
        // Arrange
        var userId = await _postgres.CreateUserAsync();
        var otherUserId = await _postgres.CreateUserAsync();
        var start = new DateOnly(2026, 1, 1);
        await CreateAsync(new RecurringBill(userId, "Open ended", 10, AmountType.Fixed, start));
        await CreateAsync(new RecurringBill(userId, "Ends this month", 10, AmountType.Fixed, start) { EndMonth = new DateOnly(2026, 3, 1) });
        await CreateAsync(new RecurringBill(userId, "Ended", 10, AmountType.Fixed, start) { EndMonth = new DateOnly(2026, 2, 1) });
        await CreateAsync(new RecurringBill(otherUserId, "Other user", 10, AmountType.Fixed, start));

        // Act
        List<RecurringBill> result;
        await using (var context = _postgres.CreateContext())
        {
            result = await new RecurringBillRepository(context).GetActive(userId, new DateOnly(2026, 3, 1));
        }

        // Assert
        result.Select(r => r.Name).Should().Equal("Open ended", "Ends this month");
    }

    [Fact]
    public async Task Update_WithRecurringBillOfUser_PersistsChanges()
    {
        // Arrange
        var userId = await _postgres.CreateUserAsync();
        var created = await CreateAsync(new RecurringBill(userId, "Rent", 10, AmountType.Fixed, new DateOnly(2026, 1, 1)));
        created.Name = "Power";
        created.AmountType = AmountType.VariableEstimated;
        created.EndMonth = new DateOnly(2026, 6, 1);

        // Act
        bool updated;
        await using (var context = _postgres.CreateContext())
        {
            updated = await new RecurringBillRepository(context).Update(created);
        }

        // Assert
        updated.Should().BeTrue();
        (await GetByIdAsync(created.Id, userId)).Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task Update_WithRecurringBillOfOtherUser_ReturnsFalseAndKeepsBill()
    {
        // Arrange
        var userId = await _postgres.CreateUserAsync();
        var otherUserId = await _postgres.CreateUserAsync();
        var created = await CreateAsync(new RecurringBill(userId, "Rent", 10, AmountType.Fixed, new DateOnly(2026, 1, 1)));
        var tampered = new RecurringBill(otherUserId, "Tampered", 10, AmountType.Fixed, created.StartMonth) { Id = created.Id };

        // Act
        bool updated;
        await using (var context = _postgres.CreateContext())
        {
            updated = await new RecurringBillRepository(context).Update(tampered);
        }

        // Assert
        updated.Should().BeFalse();
        (await GetByIdAsync(created.Id, userId))!.Name.Should().Be("Rent");
    }

    private async Task<RecurringBill> CreateAsync(RecurringBill recurringBill)
    {
        await using var context = _postgres.CreateContext();
        return await new RecurringBillRepository(context).Create(recurringBill);
    }

    private async Task<RecurringBill?> GetByIdAsync(long id, long userId)
    {
        await using var context = _postgres.CreateContext();
        return await new RecurringBillRepository(context).GetById(id, userId);
    }
}
