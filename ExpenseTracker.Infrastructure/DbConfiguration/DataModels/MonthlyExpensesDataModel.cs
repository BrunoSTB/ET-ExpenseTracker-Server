namespace ExpenseTracker.Infrastructure.DbConfiguration.DataModels
{
    public class MonthlyExpensesDataModel
    {
        public long Id { get; set; }
        public decimal TotalExpenses { get; set; }
        public DateOnly ExpensesDate { get; set; }
        public long UserId { get; set; }
        public UserDataModel? User { get; set; }
        ICollection<ExpenseDataModel>? Expenses { get; set; }
    }
}
