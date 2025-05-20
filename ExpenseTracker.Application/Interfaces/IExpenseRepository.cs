using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.Application.Interfaces
{
    public interface IExpenseRepository
    {
        Task<Expense?> GetExpenseAsync(int id);
        Task<List<MonthlyExpenses>> GetExpensesByYear(int year, long userId);
        Task<Expense?> CreateExpense(Expense expense);
        Task<bool> DeleteByIds(long[] ids, long userId);
    }
}
