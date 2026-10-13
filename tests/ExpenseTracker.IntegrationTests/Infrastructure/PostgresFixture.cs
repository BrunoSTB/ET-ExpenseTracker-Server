using ExpenseTracker.Infrastructure.DbConfiguration;
using ExpenseTracker.Infrastructure.DbConfiguration.DataModels;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(ExpenseTracker.IntegrationTests.Infrastructure.PostgresFixture))]

namespace ExpenseTracker.IntegrationTests.Infrastructure;

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public PostgresDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;
        return new PostgresDbContext(options);
    }

    public async Task<long> CreateUserAsync()
    {
        await using var context = CreateContext();
        var user = new UserDataModel($"user-{Guid.NewGuid():N}", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }
}
