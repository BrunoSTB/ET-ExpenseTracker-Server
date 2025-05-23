﻿using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Domain.Models;
using ExpenseTracker.Infrastructure.DbConfiguration;
using ExpenseTracker.Infrastructure.DbConfiguration.DataModels;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Repositories
{
    public class ExpenseRepository : IExpenseRepository
    {
        public SqlServerDbContext Context { get; }
        public IUserRepository UserRepository { get; }

        public ExpenseRepository(SqlServerDbContext context, IUserRepository userRepository)
        {
            Context = context;
            UserRepository = userRepository;
        }

        public async Task<Expense?> GetExpenseAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<Expense?> CreateExpense(Expense expense)
        {
            try
            {
                var newEntry = new ExpenseDataModel() 
                { 
                    ExpenseDate = expense.ExpenseDate, 
                    Name =  expense.Name,
                    Value = expense.Value,
                    User = Context.Users.First(x => x.Id == expense.UserId),
                    UserId = expense.UserId
                };

                var result = await Context.Expenses.AddAsync(newEntry);
                Context.SaveChanges();
                return new Expense(result.Entity.Value,
                                   result.Entity.Name!,
                                   result.Entity.ExpenseDate,
                                   result.Entity.UserId)
                       { 
                            Id = result.Entity.Id
                       };
            }
            catch (Exception ex) 
            {
                Console.WriteLine("Exception while trying to create new Expense. exception: " + ex.Message);
                return null;
            }
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

        public async Task<bool> DeleteByIds(long[] ids, long userId)
        {
            try
            {
                await Context.Expenses
                    .Where(x => ids.Contains(x.Id) && x.UserId == userId)
                    .ExecuteDeleteAsync();
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception in repository while trying to delete by multiple Ids: " + ex.Message);
                return false;
            }
        }
    }
}
