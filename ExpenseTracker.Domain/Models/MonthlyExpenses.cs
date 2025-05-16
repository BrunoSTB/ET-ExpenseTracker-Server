namespace ExpenseTracker.Domain.Models
{
    public class MonthlyExpenses
    {
        public decimal TotalExpenses { get; set; }
        public int ExpensesMonth { get; set; }
        public List<Expense> Expenses { get; set; }

        public MonthlyExpenses(List<Expense> expenses,
                               int expensesMonth)
        {
            ExpensesMonth = expensesMonth;
            Expenses = expenses;
            TotalExpenses = expenses.Sum(x => x.Value);
        }
    }
}
