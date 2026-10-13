using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.API.Controllers.RequestModels
{
    public class PaymentRequestModel
    {
        [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
        public decimal? Value { get; set; }
    }
}
