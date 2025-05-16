using ExpenseTracker.Domain.Models;
namespace ExpenseTracker.Application.Services.ExpenseService
{
    public interface IExpenseService
    {
        Task<Expense?> GetExpenseById(int id);
        Task<List<MonthlyExpenses>> GetExpensesByYear(int year, long userId);
        Task<Expense?> CreateExpense(Expense expense);
    }
}
