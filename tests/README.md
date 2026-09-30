# Testes

Toda a estrutura de testes do servidor fica em `tests/`, separada por **tipo de teste** e organizada internamente por **camada**.

```
tests/
├── Directory.Build.props            # configuração e pacotes comuns a todo projeto de teste
├── coverage.runsettings             # configuração de cobertura (exclusões)
├── README.md                        # este arquivo
├── ExpenseTracker.UnitTests/
│   ├── Domain/
│   ├── Application/
│   └── API/
└── ExpenseTracker.IntegrationTests/ # (ainda não existe, criado na #12)
```

## Ferramentas

| Função     | Pacote                                                                 |
| ---------- | ---------------------------------------------------------------------- |
| Framework  | [xUnit v3](https://xunit.net/) (`xunit.v3`)                            |
| Mocks      | [NSubstitute](https://nsubstitute.github.io/)                          |
| Asserções  | [AwesomeAssertions](https://awesomeassertions.org/) (fork Apache 2.0 do FluentAssertions 7) |
| Cobertura  | `coverlet.collector` + [ReportGenerator](https://reportgenerator.io/)  |

> **Não** use o pacote `FluentAssertions` v8+ (licença comercial). A API do AwesomeAssertions é a mesma.

Os pacotes, `TargetFramework`, `Nullable`, `ImplicitUsings` e os usings globais (`Xunit` e `AwesomeAssertions`) vêm do `tests/Directory.Build.props`. Um projeto de teste novo só precisa de um `.csproj` com as referências de projeto:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\..\ExpenseTracker.Domain\ExpenseTracker.Domain.csproj" />
  </ItemGroup>
</Project>
```

Os projetos rodam no modo VSTest do `dotnet test` (`IsTestingPlatformApplication=false`), que é o que o `coverlet.collector` suporta.

## Como rodar

Na raiz do repositório:

```bash
# todos os testes
dotnet test

# com cobertura
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# relatório de cobertura em HTML (opcional)
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:./coverage-report -reporttypes:Html
```

O CI (`.github/workflows/ci.yml`) roda restore → build → test com cobertura em todo pull request e em push para `main`, e publica o resumo de cobertura no job summary. Ainda não há limite mínimo de cobertura (entra na #10).

## Onde colocar cada teste

- Precisa de **banco**, do **pipeline HTTP** ou de **Docker**? Vai para `ExpenseTracker.IntegrationTests`.
- Caso contrário, vai para `ExpenseTracker.UnitTests`.

## Convenções

- **Pastas e namespaces espelham o código de produção.**
  `ExpenseTracker.Application/Services/UserService/UserService.cs` →
  `tests/ExpenseTracker.UnitTests/Application/Services/UserService/UserServiceTests.cs`,
  namespace `ExpenseTracker.UnitTests.Application.Services.UserService`.
- **Uma classe de teste por classe testada**, com o nome `<Classe>Tests`.
- **Nome dos testes:** `Metodo_Cenario_ResultadoEsperado`
  (ex.: `Login_WithWrongPassword_ReturnsNull`).
- **Estrutura Arrange / Act / Assert**, com os três blocos comentados e separados por linha em branco.
- **Mockar só as fronteiras:** repositórios, I/O, relógio. Código puro (ex.: `PasswordHasher`) usa a implementação real.
- **Builders e fábricas de dados** só entram quando o segundo teste precisar deles.

### Exemplo

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
