using ExpenseTracker.Domain.Models;
using ExpenseTracker.Infrastructure.Repositories;

namespace ExpenseTracker.IntegrationTests.Infrastructure.Repositories;

public class UserRepositoryTests
{
    private readonly PostgresFixture _postgres;

    public UserRepositoryTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task CreateUser_WithoutPreferences_StoresDefaultCurrencyAndLocale()
    {
        // Arrange
        var user = new User($"user-{Guid.NewGuid():N}", "hash");

        // Act
        User created;
        await using (var context = _postgres.CreateContext())
        {
            created = await new UserRepository(context).CreateUser(user);
        }

        // Assert
        await using var readContext = _postgres.CreateContext();
        var result = await new UserRepository(readContext).GetById(created.Id);
        result!.Currency.Should().Be("BRL");
        result.Locale.Should().Be("pt-BR");
    }

    [Fact]
    public async Task CreateUser_WithPreferences_RoundTripsCurrencyAndLocale()
    {
        // Arrange
        var user = new User($"user-{Guid.NewGuid():N}", "hash") { Currency = "USD", Locale = "en-US" };

        // Act
        User created;
        await using (var context = _postgres.CreateContext())
        {
            created = await new UserRepository(context).CreateUser(user);
        }

        // Assert
        await using var readContext = _postgres.CreateContext();
        var result = await new UserRepository(readContext).GetByUsername(user.Username);
        result!.Id.Should().Be(created.Id);
        result.Currency.Should().Be("USD");
        result.Locale.Should().Be("en-US");
    }

    [Fact]
    public async Task UpdatePreferences_WithExistingUser_StoresCurrencyAndLocale()
    {
        // Arrange
        User created;
        await using (var context = _postgres.CreateContext())
        {
            created = await new UserRepository(context).CreateUser(new User($"user-{Guid.NewGuid():N}", "hash"));
        }

        // Act
        bool updated;
        await using (var context = _postgres.CreateContext())
        {
            updated = await new UserRepository(context).UpdatePreferences(created.Id, "EUR", "de-DE");
        }

        // Assert
        updated.Should().BeTrue();
        await using var readContext = _postgres.CreateContext();
        var result = await new UserRepository(readContext).GetById(created.Id);
        result!.Currency.Should().Be("EUR");
        result.Locale.Should().Be("de-DE");
    }

    [Fact]
    public async Task UpdatePreferences_WithMissingUser_ReturnsFalse()
    {
        // Arrange
        await using var context = _postgres.CreateContext();

        // Act
        var updated = await new UserRepository(context).UpdatePreferences(long.MaxValue, "EUR", "de-DE");

        // Assert
        updated.Should().BeFalse();
    }
}
