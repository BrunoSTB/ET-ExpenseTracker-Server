using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace ExpenseTracker.API.Controllers.RequestModels
{
    public class PreferencesRequestModel : IValidatableObject
    {
        private static readonly HashSet<string> CurrencyCodes = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(culture => new RegionInfo(culture.Name).ISOCurrencySymbol)
            .Where(code => code.Length == 3)
            .ToHashSet(StringComparer.Ordinal);

        [Required]
        public string Currency { get; set; } = string.Empty;

        [Required]
        [MaxLength(35)]
        public string Locale { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(Currency) && !CurrencyCodes.Contains(Currency))
            {
                yield return new ValidationResult("Currency must be an ISO 4217 code.", [nameof(Currency)]);
            }

            if (!string.IsNullOrEmpty(Locale) && FindCulture(Locale) == null)
            {
                yield return new ValidationResult("Locale must be a known culture name.", [nameof(Locale)]);
            }
        }

        public static CultureInfo? FindCulture(string name)
        {
            try
            {
                var culture = CultureInfo.GetCultureInfo(name, predefinedOnly: true);
                return culture.Name.Length == 0 ? null : culture;
            }
            catch (CultureNotFoundException)
            {
                return null;
            }
        }
    }
}
