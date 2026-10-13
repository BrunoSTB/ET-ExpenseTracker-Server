namespace ExpenseTracker.Application.Exceptions
{
    public class BillValueRequiredException : Exception
    {
        public BillValueRequiredException(long billId)
            : base($"Bill {billId} has a variable amount, so the paid value is required.")
        {
        }
    }
}
