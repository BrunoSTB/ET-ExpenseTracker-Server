using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.Application.Interfaces
{
    public interface IExpenseRepository
    {
        Task<Expense?> GetExpenseAsync(int id);
        Task<Expense?> CreateExpense(Expense expense);
    }
}
