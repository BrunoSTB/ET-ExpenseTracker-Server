using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseTracker.API.Options
{
    public class JwtOptions : IValidatableObject
    {
        public const int MinSecretBytes = 32;

        [Required(ErrorMessage = "JWT_SECRET is required.")]
        public string Secret { get; set; } = string.Empty;

        [Required(ErrorMessage = "JWT_ISSUER is required.")]
        public string Issuer { get; set; } = "ExpenseTracker.API";

        [Required(ErrorMessage = "JWT_AUDIENCE is required.")]
        public string Audience { get; set; } = "ExpenseTracker.Client";

        [Range(1, int.MaxValue, ErrorMessage = "JWT_EXPIRATION_MINUTES must be a positive integer.")]
        public int ExpirationMinutes { get; set; } = 60 * 24 * 30;

        public SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(Secret));

        public void LoadFromEnvironment()
        {
            Secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? string.Empty;
            Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? Issuer;
            Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? Audience;

            var expiration = Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES");
            if (expiration != null)
            {
                ExpirationMinutes = int.TryParse(expiration, out var minutes) ? minutes : 0;
            }
        }

        public TokenValidationParameters CreateValidationParameters()
        {
            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = SigningKey,
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(Secret) && Encoding.UTF8.GetByteCount(Secret) < MinSecretBytes)
            {
                yield return new ValidationResult(
                    $"JWT_SECRET must be at least {MinSecretBytes} bytes long.",
                    [nameof(Secret)]);
            }
        }
    }
}
