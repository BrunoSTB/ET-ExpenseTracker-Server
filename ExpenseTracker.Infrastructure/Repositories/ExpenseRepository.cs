using ExpenseTracker.Application.IRepositories;
using ExpenseTracker.Domain.Models;
using ExpenseTracker.Infrastructure.DbConfiguration;
using ExpenseTracker.Infrastructure.DbConfiguration.DataModels;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Repositories
{
    public class ExpenseRepository : IExpenseRepository
    {
        public PostgresDbContext Context { get; }

        public ExpenseRepository(PostgresDbContext context)
        {
            Context = context;
        }

        public async Task<Expense?> GetExpenseAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<Expense> CreateExpense(Expense expense)
        {
            var newEntry = new ExpenseDataModel()
            {
                ExpenseDate = expense.ExpenseDate,
                Name = expense.Name,
                Value = expense.Value,
                UserId = expense.UserId
            };

            var result = await Context.Expenses.AddAsync(newEntry);
            await Context.SaveChangesAsync();
            return new Expense(result.Entity.Value,
                               result.Entity.Name,
                               result.Entity.ExpenseDate,
                               result.Entity.UserId)
                   {
                        Id = result.Entity.Id
                   };
        }

        public async Task<List<MonthlyExpenses>> GetExpensesByYear(int year, long userId)
        {
            var result = await Context.Expenses
                .Where(x => x.UserId == userId &&
                            x.ExpenseDate.Year == year)
                .GroupBy(x => x.ExpenseDate.Month)
                .Select(x => new MonthlyExpenses(x.Select(y => new Expense(y.Value, y.Name!, y.ExpenseDate, userId) { Id = y.Id }).ToList(), x.Key)).ToListAsync();
            return result;
        }

        public async Task<int> DeleteByIds(long[] ids, long userId)
        {
            return await Context.Expenses
                .Where(x => ids.Contains(x.Id) && x.UserId == userId)
                .ExecuteDeleteAsync();
        }
    }
}
