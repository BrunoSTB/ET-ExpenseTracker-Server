namespace ExpenseTracker.Domain.Models
{
    public class MonthlyExpenses
    {
        public decimal TotalExpenses { get; set; }
        public DateOnly ExpensesDate { get; set; }
        public List<Expense> Expenses { get; set; }

        public MonthlyExpenses(decimal totalExpenses,
                               DateOnly expensesDate)
        {
            TotalExpenses = totalExpenses;
            ExpensesDate = expensesDate;
            Expenses = new List<Expense>();
        }
    }
}
