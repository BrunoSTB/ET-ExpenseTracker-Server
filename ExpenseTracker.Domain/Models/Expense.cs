namespace ExpenseTracker.Domain.Models
{
    public class Expense
    {
        public long Id { get; set; }
        public decimal Value { get; set; }
        public string Name { get; set; }
        public DateTime ExpenseDate { get; set; }
        public long UserId { get; set; }

        public Expense(decimal value, string name, DateTime expenseDate, long userId)
        {
            Value = value;
            Name = name;
            ExpenseDate = expenseDate;
            UserId = userId;
        }
    }
}
