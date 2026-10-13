namespace ExpenseTracker.API.Controllers.RequestModels
{
    public class CreateRecurringBillRequestModel : RecurringBillRequestModel
    {
        public DateOnly? StartMonth { get; set; }
    }
}
