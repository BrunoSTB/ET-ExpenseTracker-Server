using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.Application.Services.ExpenseService
{
    public class ExpenseService : IExpenseService
    {
        private IExpenseRepository _expenseRepository { get; set; }

        public ExpenseService(IExpenseRepository expenseRepository)
        {
            _expenseRepository = expenseRepository;
        }

        public async Task<Expense> GetExpenseById(int id)
        {
            return await _expenseRepository.GetExpenseAsync(id);
        }
    }
}
