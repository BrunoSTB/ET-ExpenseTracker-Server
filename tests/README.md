# Tests

All server tests live under `tests/`, split by **test type** and organized internally by **layer**.

```
tests/
├── Directory.Build.props            # configuration and packages shared by every test project
├── coverage.runsettings             # coverage configuration (exclusions)
├── README.md                        # this file
├── ExpenseTracker.UnitTests/
│   ├── Domain/
│   ├── Application/
│   └── API/
└── ExpenseTracker.IntegrationTests/ # (does not exist yet, created in #12)
```

## Tooling

| Purpose    | Package                                                                |
| ---------- | ---------------------------------------------------------------------- |
| Framework  | [xUnit v3](https://xunit.net/) (`xunit.v3`)                            |
| Mocks      | [NSubstitute](https://nsubstitute.github.io/)                          |
| Assertions | [AwesomeAssertions](https://awesomeassertions.org/) (Apache 2.0 fork of FluentAssertions 7) |
| Coverage   | `coverlet.collector` + [ReportGenerator](https://reportgenerator.io/)  |

> **Do not** use the `FluentAssertions` v8+ package (commercial license). AwesomeAssertions has the same API.

The packages, `TargetFramework`, `Nullable`, `ImplicitUsings` and the global usings (`Xunit` and `AwesomeAssertions`) come from `tests/Directory.Build.props`. A new test project only needs a `.csproj` with its project references:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\..\ExpenseTracker.Domain\ExpenseTracker.Domain.csproj" />
  </ItemGroup>
</Project>
```

The projects run in `dotnet test`'s VSTest mode (`IsTestingPlatformApplication=false`), which is what `coverlet.collector` supports.

## Running

From the repository root:

```bash
# all tests
dotnet test

# with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# HTML coverage report (optional)
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:./coverage-report -reporttypes:Html
```

CI (`.github/workflows/ci.yml`) runs restore → build → test with coverage on every pull request and on pushes to `main`, and publishes the coverage summary to the job summary. The job fails if line coverage of the `ExpenseTracker.Domain` or `ExpenseTracker.Application` assembly drops below 60% (`MIN_LINE_COVERAGE` in the "Enforce coverage threshold" step).

## Where each test goes

- Does it need a **database**, the **HTTP pipeline** or **Docker**? It goes in `ExpenseTracker.IntegrationTests`.
- Otherwise, it goes in `ExpenseTracker.UnitTests`.

## Conventions

- **Folders and namespaces mirror the production code.**
  `ExpenseTracker.Application/Services/UserService/UserService.cs` →
  `tests/ExpenseTracker.UnitTests/Application/Services/UserService/UserServiceTests.cs`,
  namespace `ExpenseTracker.UnitTests.Application.Services.UserService`.
- **One test class per class under test**, named `<Class>Tests`.
- **Test names:** `Method_Scenario_ExpectedResult`
  (e.g. `Login_WithWrongPassword_ReturnsNull`).
- **Arrange / Act / Assert structure**, with the three blocks commented and separated by a blank line.
- **Only mock boundaries:** repositories, I/O, the clock. Pure code (e.g. `PasswordHasher`) uses the real implementation.
- **Data builders and factories** are only added when a second test needs them.

### Example

```csharp
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Domain.Models;
using NSubstitute;

namespace ExpenseTracker.UnitTests.Application.Services.UserService;

public class UserServiceTests
{
    [Fact]
    public async Task GetUserById_WhenUserExists_ReturnsUser()
    {
        // Arrange
        var user = new User("bruno") { Id = 1 };
        var repository = Substitute.For<IUserRepository>();
        repository.GetById(1).Returns(user);
        var service = new ExpenseTracker.Application.Services.UserService.UserService(repository);

        // Act
        var result = await service.GetUserById(1);

        // Assert
        result.Should().BeSameAs(user);
    }
}
```
