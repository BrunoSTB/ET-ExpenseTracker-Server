using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.API.Options
{
    public class CorsOptions : IValidatableObject
    {
        public const string Variable = "CORSOrigins";
        public const string PolicyName = "AllowAngularDev";

        [MinLength(1, ErrorMessage = "CORSOrigins is required. Set it to a comma-separated list of allowed origins.")]
        public string[] Origins { get; set; } = [];

        public void Load(IConfiguration configuration)
        {
            Origins = (configuration[Variable] ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var origin in Origins)
            {
                if (!IsValidOrigin(origin))
                {
                    yield return new ValidationResult(
                        $"CORSOrigins contains an invalid origin '{origin}'. Use scheme://host[:port] with http or https and no path or trailing slash.",
                        [nameof(Origins)]);
                }
            }
        }

        private static bool IsValidOrigin(string origin)
        {
            return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                && uri.PathAndQuery == "/"
                && string.IsNullOrEmpty(uri.Fragment)
                && string.IsNullOrEmpty(uri.UserInfo)
                && !origin.EndsWith('/');
        }
    }
}
