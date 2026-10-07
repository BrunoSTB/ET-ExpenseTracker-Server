using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.Application.IRepositories
{
    public interface IExpenseRepository
    {
        Task<Expense?> GetExpenseAsync(int id);
        Task<List<MonthlyExpenses>> GetExpensesByYear(int year, long userId);
        Task<Expense> CreateExpense(Expense expense);
        Task<int> DeleteByIds(long[] ids, long userId);
    }
}
