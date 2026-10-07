using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ExpenseTracker.API.Configuration;
using ExpenseTracker.API.Services;
using ExpenseTracker.Domain.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseTracker.UnitTests.API.Services;

public class JwtTokenServiceTests
{
    private readonly JwtOptions _options = new()
    {
        Secret = "a-test-secret-that-is-at-least-32-bytes-long",
        Issuer = "expense-tracker-tests",
        Audience = "expense-tracker-tests-client",
        ExpirationMinutes = 60
    };

    private readonly FixedTimeProvider _timeProvider = new(DateTimeOffset.UtcNow);
    private readonly User _user = new("bruno", "hashed-password") { Id = 42 };

    [Fact]
    public void GenerateToken_WithUser_ContainsUserIdAndUsernameClaims()
    {
        // Arrange
        var service = CreateService();

        // Act
        var token = new JwtSecurityTokenHandler().ReadJwtToken(service.GenerateToken(_user));

        // Assert
        var principal = ValidateToken(token.RawData, _options.CreateTokenValidationParameters());
        principal.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be("42");
        principal.FindFirst(ClaimTypes.Name)!.Value.Should().Be("bruno");
    }

    [Fact]
    public void GenerateToken_WithOptions_SetsIssuerAudienceAndExpiration()
    {
        // Arrange
        var service = CreateService();
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Act
        var token = new JwtSecurityTokenHandler().ReadJwtToken(service.GenerateToken(_user));

        // Assert
        token.Issuer.Should().Be(_options.Issuer);
        token.Audiences.Should().Equal(_options.Audience);
        token.ValidTo.Should().BeCloseTo(now.AddMinutes(_options.ExpirationMinutes), TimeSpan.FromSeconds(1));
        token.SignatureAlgorithm.Should().Be(SecurityAlgorithms.HmacSha256);
    }

    [Fact]
    public void GenerateToken_ValidatedWithOtherAudience_IsRejected()
    {
        // Arrange
        var service = CreateService();
        var parameters = _options.CreateTokenValidationParameters();
        parameters.ValidAudience = "another-client";

        // Act
        var act = () => ValidateToken(service.GenerateToken(_user), parameters);

        // Assert
        act.Should().Throw<SecurityTokenInvalidAudienceException>();
    }

    [Fact]
    public void GenerateToken_ValidatedWithOtherIssuer_IsRejected()
    {
        // Arrange
        var service = CreateService();
        var parameters = _options.CreateTokenValidationParameters();
        parameters.ValidIssuer = "another-issuer";

        // Act
        var act = () => ValidateToken(service.GenerateToken(_user), parameters);

        // Assert
        act.Should().Throw<SecurityTokenInvalidIssuerException>();
    }

    [Fact]
    public void GenerateToken_ValidatedWithOtherSecret_IsRejected()
    {
        // Arrange
        var service = CreateService();
        var parameters = _options.CreateTokenValidationParameters();
        parameters.IssuerSigningKey = new JwtOptions { Secret = "another-secret-that-is-also-32-bytes-long" }.CreateSigningKey();

        // Act
        var act = () => ValidateToken(service.GenerateToken(_user), parameters);

        // Assert
        act.Should().Throw<SecurityTokenInvalidSignatureException>();
    }

    [Fact]
    public void GenerateToken_AfterExpiration_IsRejected()
    {
        // Arrange
        var service = new JwtTokenService(Options.Create(_options), new FixedTimeProvider(DateTimeOffset.UtcNow.AddMinutes(-_options.ExpirationMinutes - 1)));

        // Act
        var act = () => ValidateToken(service.GenerateToken(_user), _options.CreateTokenValidationParameters());

        // Assert
        act.Should().Throw<SecurityTokenExpiredException>();
    }

    private JwtTokenService CreateService()
    {
        return new JwtTokenService(Options.Create(_options), _timeProvider);
    }

    private static ClaimsPrincipal ValidateToken(string token, TokenValidationParameters parameters)
    {
        return new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
