using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Domain.Models;
using ExpenseTracker.Infrastructure.DbConfiguration;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Repositories
{
    public class ExpenseRepository : IExpenseRepository
    {
        public PsqlDbContext Context { get; }

        public ExpenseRepository(PsqlDbContext context)
        {
            Context = context;
        }

        public async Task<Expense> GetExpenseAsync(int id)
        {
            var expenseDataModel = await Context.Expenses.FirstOrDefaultAsync(x => x.Id == id);
            return new Expense(expenseDataModel.Value, expenseDataModel.Name, expenseDataModel.ExpenseDate);
        }
    }
}
