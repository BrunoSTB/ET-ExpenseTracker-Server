namespace ExpenseTracker.Domain.Models
{
    public class RecurringBill
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Name { get; set; }
        public int DueDay { get; set; }
        public AmountType AmountType { get; set; }
        public decimal? FixedValue { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly? EndMonth { get; set; }

        public RecurringBill(long userId, string name, int dueDay, AmountType amountType, DateOnly startMonth)
        {
            UserId = userId;
            Name = name;
            DueDay = dueDay;
            AmountType = amountType;
            StartMonth = startMonth;
        }
    }
}
