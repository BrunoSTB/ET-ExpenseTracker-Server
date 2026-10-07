using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseTracker.API.Configuration
{
    public class JwtOptions : IValidatableObject
    {
        public const string SectionName = "Jwt";
        public const int MinimumSecretBytes = 32;
        public const int MaximumExpirationMinutes = 43200;

        [Required(ErrorMessage = "Jwt:Secret is required (environment variable Jwt__Secret).")]
        public string Secret { get; set; } = string.Empty;

        [Required(ErrorMessage = "Jwt:Issuer is required (environment variable Jwt__Issuer).")]
        public string Issuer { get; set; } = string.Empty;

        [Required(ErrorMessage = "Jwt:Audience is required (environment variable Jwt__Audience).")]
        public string Audience { get; set; } = string.Empty;

        [Range(1, MaximumExpirationMinutes, ErrorMessage = "Jwt:ExpirationMinutes must be between {1} and {2} (environment variable Jwt__ExpirationMinutes).")]
        public int ExpirationMinutes { get; set; }

        public SymmetricSecurityKey CreateSigningKey()
        {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        }

        public TokenValidationParameters CreateTokenValidationParameters()
        {
            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = CreateSigningKey(),
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.Zero
            };
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Encoding.UTF8.GetByteCount(Secret) < MinimumSecretBytes)
            {
                yield return new ValidationResult(
                    $"Jwt:Secret must be at least {MinimumSecretBytes} bytes long (HS256 requires a 256-bit key).",
                    [nameof(Secret)]);
            }
        }
    }
}
