using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ExpenseTracker.API.Options;
using ExpenseTracker.API.Services;
using ExpenseTracker.Domain.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseTracker.UnitTests.API.Services;

public class TokenServiceTests
{
    private readonly JwtOptions _options = new()
    {
        Secret = "a-test-secret-that-is-at-least-32-bytes-long",
        Issuer = "test-issuer",
        Audience = "test-audience",
        ExpirationMinutes = 15
    };

    private readonly User _user = new("bruno", "hashed-password") { Id = 42 };

    private TokenService CreateService() => new(new OptionsWrapper<JwtOptions>(_options));

    [Fact]
    public void GenerateToken_ForUser_IsValidForConfiguredValidationParameters()
    {
        // Arrange
        var token = CreateService().GenerateToken(_user);

        // Act
        var principal = new JwtSecurityTokenHandler()
            .ValidateToken(token, _options.CreateValidationParameters(), out _);

        // Assert
        principal.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be("42");
        principal.FindFirstValue(ClaimTypes.Name).Should().Be("bruno");
    }

    [Fact]
    public void GenerateToken_ForUser_UsesConfiguredIssuerAudienceAndExpiration()
    {
        // Act
        var token = new JwtSecurityTokenHandler().ReadJwtToken(CreateService().GenerateToken(_user));

        // Assert
        token.Issuer.Should().Be("test-issuer");
        token.Audiences.Should().Equal("test-audience");
        token.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateToken_ValidatedWithDifferentAudience_Throws()
    {
        // Arrange
        var token = CreateService().GenerateToken(_user);
        var parameters = _options.CreateValidationParameters();
        parameters.ValidAudience = "another-audience";

        // Act
        var act = () => new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);

        // Assert
        act.Should().Throw<SecurityTokenInvalidAudienceException>();
    }

    [Fact]
    public void GenerateToken_ValidatedWithDifferentSecret_Throws()
    {
        // Arrange
        var token = CreateService().GenerateToken(_user);
        var otherOptions = new JwtOptions
        {
            Secret = "another-secret-that-is-at-least-32-bytes-long",
            Issuer = _options.Issuer,
            Audience = _options.Audience
        };

        // Act
        var act = () => new JwtSecurityTokenHandler().ValidateToken(token, otherOptions.CreateValidationParameters(), out _);

        // Assert
        act.Should().Throw<SecurityTokenException>();
    }
}
