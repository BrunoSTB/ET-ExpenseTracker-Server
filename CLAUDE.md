# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

ASP.NET Core Web API (.NET 10, EF Core 10 + PostgreSQL, JWT auth) backing a bills-to-pay tracker frontend (Angular dev origin by default).

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

Migrations are applied automatically on startup (`db.Database.Migrate()` in `Program.cs`, guarded by `Database:MigrateOnStartup`, default `true`), so there is no manual `database update` step.

CI (`.github/workflows/ci.yml`) runs restore → build (Release) → test with coverage on every PR and push to `main`, and fails if line coverage of the `Domain` or `Application` assembly is below 60%. There is no lint step.

`.github/workflows/docker.yml` builds the `Dockerfile` on every push to `main` and pushes it to GHCR (`ghcr.io/brunostb/et-expensetracker-server`, tagged with the commit SHA and `latest`). Deploying to Azure Container Apps is a manual `az containerapp update --image` with the SHA tag (see README).

## Runtime configuration

Configuration comes from **environment variables**, loaded into typed options classes in `API/Options` that are validated with `ValidateDataAnnotations` + `ValidateOnStart`. `Program.cs` runs `IStartupValidator.Validate()` right after `Build()`, so every missing/invalid setting is reported at once, before migrations touch the database:

- `ConnectionStrings__DefaultConnection` — Npgsql connection string, read via `IConfiguration` into `API/Options/DatabaseOptions.cs`. Required, must parse and specify a `Host`; there is no default and no credentials in code.
- `Database__MigrateOnStartup` — optional, default `true`; the integration tests set it to `false` so the host starts without Postgres.
- `JWT_SECRET` (≥ 32 bytes), `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_EXPIRATION_MINUTES` — read via `IConfiguration` into `API/Options/JwtOptions.cs` (validated with `ValidateDataAnnotations` + `ValidateOnStart`, so a missing/short secret fails at startup). `JwtOptions` is the single source for both token signing (`API/Services/TokenService.cs`, injected as `ITokenService`) and JWT bearer validation in `Program.cs`. Only the secret is required; it is intentionally absent from `launchSettings.json` (launch-profile variables override the shell, so even an empty entry would hide an exported secret) and must be exported locally.
- `CORSOrigins` — comma-separated, read via `IConfiguration` into `API/Options/CorsOptions.cs`. Required; each origin must be `http(s)://host[:port]` with no path or trailing slash.

The default `dotnet run` profile is `http` (port 5149, no Swagger env); use `--launch-profile https` for local dev.

## Architecture

Clean Architecture, four projects. Dependency direction matters:

- **Domain** — plain models, no dependencies:
  - `RecurringBill` — a bill template (`Name`, `DueDay` 1–31, `AmountType`, `FixedValue?`, `StartMonth`, `EndMonth?`). Months are `DateOnly` on the first day of the month. It is never paid; it only generates `Bill`s. It is active from `StartMonth` (inclusive) to `EndMonth` (exclusive); `IsActiveIn`, `DueDateIn` (a due day missing from the month falls on its last day) and `CreateBillFor` hold those rules. Removing one is a soft delete (`EndMonth` set to the current month).
  - `Bill` — one bill of one month (`DueDate`, `Value?`, `PaidAt?`, `SkippedAt?`). `RecurringBillId` set = generated from a template; null = standalone. Payment works the same for both. `SkippedAt` set = that month of a template was removed (soft delete). Status (late, due soon, pending) is not stored; the UI derives it from today's date.
  - `AmountType` — `Fixed`, `VariableEstimated`, `VariableOnArrival`.
  - `User` — includes the `Currency` (ISO 4217, default `BRL`) and `Locale` (default `pt-BR`) preferences. The server never translates; dates go out as ISO `DateOnly` and amounts as `decimal`.
- **Application** — services and their interfaces flat in `Services/` (namespace `ExpenseTracker.Application.Services`), the **repository interfaces** in `IRepositories/` (namespace `ExpenseTracker.Application.IRepositories`), and service result types in `Models/` (e.g. `BillOverview`, a bill plus its estimate). Folder = namespace, one type per file, no per-class subfolders. Password hashing uses `PasswordHasher<User>` from `Microsoft.Extensions.Identity.Core` here.
- **Infrastructure** — references Application to implement its repository interfaces (`IUserRepository`, `IRecurringBillRepository`, `IBillRepository`). Owns `PostgresDbContext`, EF migrations, and separate EF entities in `DbConfiguration/DataModels` (`UserDataModel`, `RecurringBillDataModel`, `BillDataModel`). **Repositories map manually between DataModels and Domain models** — Domain types are never tracked by EF, so a new field must be added to both the Domain model and the DataModel (plus a migration) and to the mapping code in the repository.
- **API** — controllers, request models (`Controllers/RequestModels`), response DTOs (`Dtos`, namespace `ExpenseTracker.API.Dtos`), JWT generation, and all DI wiring in `Program.cs` (scoped registrations per repository/service).

Request flow: Controller builds a Domain model from a RequestModel → Service (business rules) → Repository (maps to DataModel, hits EF).

Bills flow: `GET Bill?year=` (`BillService.GetYear`) creates the missing bills of every active template for the 12 months of the year, then returns the year's bills grouped by month with an `estimate` for `VariableEstimated` bills (average of the template's last 3 paid values, null without history). Editing a template (`PUT RecurringBill/{id}`) also rewrites, in one `UPDATE`, its unpaid bills from the current month on; removing it (`DELETE RecurringBill/{id}`) deletes those unpaid bills, plus its skipped bills from the current month on. Paid bills and past months never change. Skipped bills are ignored everywhere: not returned by `GetByYear` or `GetById` (so payment endpoints answer `404`), left out of estimates, and never touched by `Update` or `UpdateUnpaidFrom`. The current month comes from the injected `TimeProvider` (UTC), so tests fix the date.

Payment: `PUT Bill/{id}/Payment` `{ value? }` sets `PaidAt` to today (UTC, from `TimeProvider`) in any month, future ones included; `value` is required for variable bills (`BillValueRequiredException` → `400`) and, for `Fixed`, overwrites the value when sent. `DELETE Bill/{id}/Payment` clears `PaidAt`, and variable bills go back to `Value = null`. Both return the bill (with its `estimate`). Standalone bills are created with `POST Bill` (`Fixed` requires `value`, variable types reject it) and removed with `DELETE Bill/{id}` (hard delete). `DELETE Bill/{id}` on a bill generated from a template skips only that month: it sets `SkippedAt` to today instead of deleting the row, so the row keeps its `(RecurringBillId, Period)` slot and `CreateMissing` does not recreate it; the template keeps going. Deleting an already skipped bill answers `404`.

Preferences: `GET`/`PUT User/Preferences` read and write `{ currency, locale }`. `PreferencesRequestModel` accepts only ISO 4217 codes known to .NET's `RegionInfo` (uppercase) and predefined `CultureInfo` names (any casing, stored in canonical form, e.g. `pt-BR`); this needs ICU, so do not enable `InvariantGlobalization`. Enums are serialized as strings (`JsonStringEnumConverter`, integers rejected).

Bill storage: `BillDataModel` has a `Period` column (first day of the `DueDate` month, filled by the repository) with a unique index on `(RecurringBillId, Period)`, so a template has at most one bill per month; standalone bills (null `RecurringBillId`) are unconstrained. `IBillRepository.CreateMissing` inserts with `ON CONFLICT DO NOTHING`, which makes generating a year idempotent. A skipped bill (`SkippedAt` set) stays in the table holding its slot, which is why skipping a month survives regeneration. `AmountType` is stored as a string; `RecurringBills.DueDay` has a `1–31` check constraint; `Bills.RecurringBillId` is a FK with `NO ACTION` on delete (templates are never hard-deleted).

Auth: controllers use `[Authorize]`; the current user id is read from the `ClaimTypes.NameIdentifier` claim (`User.GetUserId()` from `API/Helpers/ClaimsPrincipalExtensions.cs`); never take a user id from the request. Every repository read, update and delete of bills and templates takes that user id and filters by it, so another user's id behaves like a missing row. Tokens validate signature, issuer, audience and lifetime; they last 30 days by default.

Error handling convention: repositories do not catch exceptions (the one exception is a Postgres unique violation on user insert, translated to `UsernameAlreadyTakenException`); anything unexpected propagates to `API/Handlers/GlobalExceptionHandler.cs` (`IExceptionHandler`), which logs it via `ILogger` and answers `500` as ProblemDetails. `AddProblemDetails` + `UseStatusCodePages` make every error response `application/problem+json`; controllers return errors with `Problem(statusCode: ...)` or the bodiless helpers (`NotFound()`), never plain strings. Status codes: `201` on create, `401` on invalid login, `409` on a taken username (`UserController` catches `UsernameAlreadyTakenException`), `404` only when the resource really does not exist or belongs to another user. Do not use `Console.WriteLine`; use the injected `ILogger`. Use async EF APIs (`SaveChangesAsync`, `FirstOrDefaultAsync`) inside async methods. Controllers return DTOs (`API/Dtos`), never the `User` domain model, so the password hash is never serialized.

## Tests

Details in `tests/README.md`. Key points:

- xUnit v3 + NSubstitute + **AwesomeAssertions** (do not add FluentAssertions v8+, it is commercially licensed). Packages, target framework and global usings (`Xunit`, `AwesomeAssertions`) come from `tests/Directory.Build.props`; a new test `.csproj` only needs project references.
- Tests run in VSTest mode (`IsTestingPlatformApplication=false`) for `coverlet.collector` compatibility. Migrations are excluded from coverage via `tests/coverage.runsettings`.
- Unit tests go in `tests/ExpenseTracker.UnitTests`; anything needing a database, the HTTP pipeline or Docker goes in `tests/ExpenseTracker.IntegrationTests`.
- Repository tests (`tests/ExpenseTracker.IntegrationTests/Infrastructure/Repositories`) run against a real PostgreSQL started by Testcontainers (`PostgresFixture`, an xUnit assembly fixture that applies the migrations once). They need a running Docker daemon. Tests isolate themselves by creating their own users (`PostgresFixture.CreateUserAsync`) instead of cleaning tables.
- API integration tests (`tests/ExpenseTracker.IntegrationTests/Api`) use `CustomWebApplicationFactory` (in-memory pipeline, migrations off, test settings via `UseSetting`, repositories replaced by NSubstitute substitutes exposed on the factory) and `TestTokens` (valid tokens from the API's `ITokenService`, plus wrong issuer/audience/expired/other-key tokens). Each test class owns and disposes its factory. New endpoints or changes to status codes, validation or auth get a test there.
- Folders and namespaces mirror production code (e.g. `Application/Services/UserService.cs` → `tests/ExpenseTracker.UnitTests/Application/Services/UserServiceTests.cs`, namespace `ExpenseTracker.UnitTests.Application.Services`). One `<Class>Tests` per class; names follow `Method_Scenario_ExpectedResult`; Arrange/Act/Assert blocks are commented.
- Mock only boundaries (repositories, I/O, clock); use real implementations for pure code like `PasswordHasher`. Add builders/factories only once a second test needs them.
- Test files import the production namespace with `using` (e.g. `using ExpenseTracker.Application.Services;`) and reference the type under test by its simple name.



## Code Conventions
- Avoid Comments.