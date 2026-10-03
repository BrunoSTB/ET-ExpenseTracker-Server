using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.API.Controllers.RequestModels
{
    public class CreateExpenseRequestModel
    {
        private DateTime? _expenseDate;

        [Required]
        public string Name { get; set; } = string.Empty;

        [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ParseLimitsInInvariantCulture = true)]
        public decimal Value { get; set; }

        [Required]
        [Range(typeof(DateTime), "1900-01-01", "9999-12-31T23:59:59", ParseLimitsInInvariantCulture = true, ErrorMessage = "The ExpenseDate field must be between 1900-01-01 and 9999-12-31.")]
        public DateTime? ExpenseDate
        {
            get => _expenseDate ?? Date;
            set => _expenseDate = value;
        }

        public DateTime? Date { get; set; }
    }
}
