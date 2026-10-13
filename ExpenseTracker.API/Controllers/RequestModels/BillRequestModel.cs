using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.API.Controllers.RequestModels
{
    public class BillRequestModel : IValidatableObject
    {
        public const int MinYear = 2000;
        public const int MaxYear = 2999;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public DateOnly? DueDate { get; set; }

        [Required]
        public AmountType? AmountType { get; set; }

        [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
        public decimal? Value { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DueDate != null && (DueDate.Value.Year < MinYear || DueDate.Value.Year > MaxYear))
            {
                yield return new ValidationResult($"DueDate must be between {MinYear} and {MaxYear}.", [nameof(DueDate)]);
            }

            if (AmountType == Domain.Models.AmountType.Fixed && Value == null)
            {
                yield return new ValidationResult("Value is required when AmountType is Fixed.", [nameof(Value)]);
            }

            if (AmountType != null && AmountType != Domain.Models.AmountType.Fixed && Value != null)
            {
                yield return new ValidationResult("Value must be empty when AmountType is variable.", [nameof(Value)]);
            }
        }
    }
}
