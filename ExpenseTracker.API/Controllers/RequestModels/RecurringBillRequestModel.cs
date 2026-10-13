using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.API.Controllers.RequestModels
{
    public class RecurringBillRequestModel : IValidatableObject
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Range(1, 31)]
        public int DueDay { get; set; }

        [Required]
        public AmountType? AmountType { get; set; }

        [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
        public decimal? FixedValue { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (AmountType == Domain.Models.AmountType.Fixed && FixedValue == null)
            {
                yield return new ValidationResult("FixedValue is required when AmountType is Fixed.", [nameof(FixedValue)]);
            }

            if (AmountType != Domain.Models.AmountType.Fixed && FixedValue != null)
            {
                yield return new ValidationResult("FixedValue must be empty when AmountType is variable.", [nameof(FixedValue)]);
            }
        }
    }
}
