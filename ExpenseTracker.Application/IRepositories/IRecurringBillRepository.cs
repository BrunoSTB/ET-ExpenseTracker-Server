using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.Application.IRepositories
{
    public interface IRecurringBillRepository
    {
        Task<RecurringBill?> GetById(long id, long userId);
        Task<List<RecurringBill>> GetActive(long userId, DateOnly fromMonth);
        Task<RecurringBill> Create(RecurringBill recurringBill);
        Task<bool> Update(RecurringBill recurringBill);
    }
}
