namespace ExpenseTracker.Domain.Models
{
    public class Bill
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long? RecurringBillId { get; set; }
        public string Name { get; set; }
        public AmountType AmountType { get; set; }
        public DateOnly DueDate { get; set; }
        public decimal? Value { get; set; }
        public DateOnly? PaidAt { get; set; }
        public DateOnly? SkippedAt { get; set; }

        public Bill(long userId, string name, AmountType amountType, DateOnly dueDate)
        {
            UserId = userId;
            Name = name;
            AmountType = amountType;
            DueDate = dueDate;
        }
    }
}
