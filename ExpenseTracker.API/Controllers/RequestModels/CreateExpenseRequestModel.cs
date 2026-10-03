using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.API.Controllers.RequestModels
{
    public class CreateExpenseRequestModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Range(typeof(decimal), "0.01", "999000000000", ParseLimitsInInvariantCulture = true)]
        public decimal Value { get; set; }

        [Required]
        [Range(typeof(DateTime), "1900-01-01", "2200-12-31T23:59:59", ParseLimitsInInvariantCulture = true, ErrorMessage = "The ExpenseDate field must be between 1900-01-01 and 2200-12-31.")]
        public DateTime? ExpenseDate { get; set; }
    }
}
