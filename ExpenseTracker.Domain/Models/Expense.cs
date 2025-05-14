namespace ExpenseTracker.Domain.Models
{
    public class Expense
    {

        public decimal Value { get; set; }

        public string Name { get; set; }
        public DateTime ExpenseDate { get; set; }

        public Expense(decimal value, string name, DateTime expenseDate)
        {
            Value = value;
            Name = name;
            ExpenseDate = expenseDate;
        }
    }
}
