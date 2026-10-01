# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

ASP.NET Core Web API (.NET 10, EF Core 10 + PostgreSQL, JWT auth) backing an expense-tracker frontend (Angular dev origin by default).

## Commands

Run from the repository root.

```bash
dotnet build ExpenseTracker.sln
dotnet test                                                        # all tests
dotnet test --filter "FullyQualifiedName~UserServiceTests"         # one class
dotnet test --filter "FullyQualifiedName~UserServiceTests.Login_WithWrongPassword_ReturnsNull"  # one test
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

dotnet run --project ExpenseTracker.API --launch-profile https     # HTTPS + Swagger at https://localhost:7010/swagger
docker compose up -d --build                                       # API + Postgres together
```

EF Core migrations live in Infrastructure but need the API as startup project:

```bash
dotnet ef migrations add <Name> --project ExpenseTracker.Infrastructure --startup-project ExpenseTracker.API
```

Migrations are applied automatically on startup (`db.Database.Migrate()` in `Program.cs`), so there is no manual `database update` step.

CI (`.github/workflows/ci.yml`) runs restore → build (Release) → test with coverage on every PR and push to `main`. There is no lint step.

## Runtime configuration

Configuration comes from **environment variables read directly via `Environment.GetEnvironmentVariable`**, not `IConfiguration`/appsettings:

- `SqlConnectionString` — Npgsql connection string (falls back to a localhost default).
- `JWT_SECRET` — used both to validate tokens (`Program.cs`) and to sign them (`API/Helpers/AuthHelpers.cs`). Intentionally blank in `launchSettings.json`; must be set locally.
- `CORSOrigins` — comma-separated; required, the app crashes at startup if unset.

The default `dotnet run` profile is `http` (port 5149, no Swagger env); use `--launch-profile https` for local dev.

## Architecture

Clean Architecture, four projects. Dependency direction matters:

- **Domain** — plain models (`Expense`, `User`, `MonthlyExpenses`) and DTOs. No dependencies.
- **Application** — services (`ExpenseService`, `UserService`) and the **repository interfaces** (`Application/Interfaces`). Password hashing uses `PasswordHasher<User>` from `Microsoft.Extensions.Identity.Core` here.
- **Infrastructure** — references Application to implement its repository interfaces. Owns `PostgresDbContext`, EF migrations, and separate EF entities in `DbConfiguration/DataModels` (`ExpenseDataModel`, `UserDataModel`). **Repositories map manually between DataModels and Domain models** — Domain types are never tracked by EF, so a new field must be added to both the Domain model and the DataModel (plus a migration) and to the mapping code in the repository.
- **API** — controllers, request models (`Controllers/RequestModels`), JWT generation, and all DI wiring in `Program.cs` (scoped registrations per repository/service).

Request flow: Controller builds a Domain model from a RequestModel → Service (mostly a thin pass-through) → Repository (maps to DataModel, hits EF).

Auth: controllers use `[Authorize]`; the current user id is read from the `ClaimTypes.NameIdentifier` claim (`GetCurrentUserId()` in `ExpenseController`). Expense queries/deletes are always scoped by that user id at the repository level. Tokens last 30 days.

Error handling convention: repositories catch exceptions, log with `Console.WriteLine`, and return `null`/`false`; controllers translate those into `NotFound`/`BadRequest`/500.

## Tests

Details in `tests/README.md`. Key points:

- xUnit v3 + NSubstitute + **AwesomeAssertions** (do not add FluentAssertions v8+, it is commercially licensed). Packages, target framework and global usings (`Xunit`, `AwesomeAssertions`) come from `tests/Directory.Build.props`; a new test `.csproj` only needs project references.
- Tests run in VSTest mode (`IsTestingPlatformApplication=false`) for `coverlet.collector` compatibility. Migrations are excluded from coverage via `tests/coverage.runsettings`.
- Unit tests go in `tests/ExpenseTracker.UnitTests`; anything needing a database, the HTTP pipeline or Docker belongs in a future `ExpenseTracker.IntegrationTests` project.
- Folders and namespaces mirror production code (e.g. `tests/ExpenseTracker.UnitTests/Application/Services/UserService/UserServiceTests.cs`). One `<Class>Tests` per class; names follow `Method_Scenario_ExpectedResult`; Arrange/Act/Assert blocks are commented.
- Mock only boundaries (repositories, I/O, clock); use real implementations for pure code like `PasswordHasher`. Add builders/factories only once a second test needs them.
- Because service namespaces end in the class name (`...Services.UserService.UserService`), tests in a matching namespace must fully qualify the type under test.
