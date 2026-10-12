using ExpenseTracker.Application.IRepositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace ExpenseTracker.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string JwtSecret = "integration-tests-secret-with-at-least-32-bytes";
    public const string JwtIssuer = "ExpenseTracker.IntegrationTests.Issuer";
    public const string JwtAudience = "ExpenseTracker.IntegrationTests.Audience";

    public IUserRepository UserRepository { get; } = Substitute.For<IUserRepository>();
    public IExpenseRepository ExpenseRepository { get; } = Substitute.For<IExpenseRepository>();

    public HttpClient CreateApiClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Database=test");
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.UseSetting("CORSOrigins", "http://localhost:4200");
        builder.UseSetting("JWT_SECRET", JwtSecret);
        builder.UseSetting("JWT_ISSUER", JwtIssuer);
        builder.UseSetting("JWT_AUDIENCE", JwtAudience);
        builder.UseSetting("JWT_EXPIRATION_MINUTES", "60");

        builder.ConfigureTestServices(services =>
        {
            services.AddScoped(_ => UserRepository);
            services.AddScoped(_ => ExpenseRepository);
        });
    }
}
