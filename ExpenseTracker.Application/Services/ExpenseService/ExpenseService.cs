﻿using ExpenseTracker.Application.Interfaces;
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

        public async Task<Expense?> GetExpenseById(int id)
        {
            return await _expenseRepository.GetExpenseAsync(id);
        }

        public async Task<Expense?> CreateExpense(Expense expense)
        {
            return await _expenseRepository.CreateExpense(expense);
        }

        public async Task<List<MonthlyExpenses>> GetExpensesByYear(int year, long userId)
        {
            return await _expenseRepository.GetExpensesByYear(year, userId);
        }

        public async Task<bool> DeleteByIds(long[] expensesIds, long userId)
        {
            return await _expenseRepository.DeleteByIds(expensesIds, userId);
        }
    }
}
