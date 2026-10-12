using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExpenseTracker.API.Services;
using ExpenseTracker.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseTracker.IntegrationTests.Infrastructure;

public static class TestTokens
{
    public static string For(CustomWebApplicationFactory factory, User user)
    {
        return factory.Services.GetRequiredService<ITokenService>().GenerateToken(user);
    }

    public static string WithWrongIssuer(long userId)
    {
        return Create(userId, issuer: "wrong-issuer");
    }

    public static string WithWrongAudience(long userId)
    {
        return Create(userId, audience: "wrong-audience");
    }

    public static string Expired(long userId)
    {
        var now = DateTime.UtcNow;
        return Create(userId, notBefore: now.AddHours(-2), expires: now.AddHours(-1));
    }

    public static string WithOtherKey(long userId)
    {
        return Create(userId, secret: "another-secret-that-is-also-at-least-32-bytes");
    }

    private static string Create(long userId,
                                 string issuer = CustomWebApplicationFactory.JwtIssuer,
                                 string audience = CustomWebApplicationFactory.JwtAudience,
                                 string secret = CustomWebApplicationFactory.JwtSecret,
                                 DateTime? notBefore = null,
                                 DateTime? expires = null)
    {
        var now = DateTime.UtcNow;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            notBefore: notBefore ?? now,
            expires: expires ?? now.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
