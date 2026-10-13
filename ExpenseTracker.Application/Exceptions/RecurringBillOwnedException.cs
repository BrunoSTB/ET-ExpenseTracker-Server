namespace ExpenseTracker.Application.Exceptions
{
    public class RecurringBillOwnedException : Exception
    {
        public RecurringBillOwnedException(long billId)
            : base($"Bill {billId} belongs to a recurring bill and can only be removed through it.")
        {
        }
    }
}
