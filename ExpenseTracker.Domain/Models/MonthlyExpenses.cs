namespace ExpenseTracker.Domain.Models
{
    public class MonthlyExpenses
    {
        public decimal TotalExpenses { get; set; }
        public DateOnly ExpensesDate { get; set; }
        public List<Expense> Expenses { get; set; }

        public MonthlyExpenses(List<Expense> expenses,
                               DateOnly expensesDate)
        {
            ExpensesDate = expensesDate;
            Expenses = expenses;
            TotalExpenses = expenses.Sum(x => x.Value);
        }
    }
}
