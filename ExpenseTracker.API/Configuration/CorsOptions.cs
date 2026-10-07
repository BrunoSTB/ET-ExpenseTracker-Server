using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.API.Configuration
{
    public class CorsOptions : IValidatableObject
    {
        public const string SectionName = "Cors";

        [Required(ErrorMessage = "Cors:AllowedOrigins is required (environment variable Cors__AllowedOrigins).")]
        public string AllowedOrigins { get; set; } = string.Empty;

        public string[] GetOrigins()
        {
            return AllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var origin in GetOrigins())
            {
                if (!IsValidOrigin(origin))
                {
                    yield return new ValidationResult(
                        $"Cors:AllowedOrigins contains '{origin}', which is not a valid origin (expected scheme://host[:port] with no path or trailing slash).",
                        [nameof(AllowedOrigins)]);
                }
            }
        }

        private static bool IsValidOrigin(string origin)
        {
            return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                && string.Equals(uri.GetLeftPart(UriPartial.Authority), origin, StringComparison.OrdinalIgnoreCase);
        }
    }
}
