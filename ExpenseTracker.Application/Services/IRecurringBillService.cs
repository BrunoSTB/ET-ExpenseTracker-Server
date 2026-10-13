using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.Application.Services
{
    public interface IRecurringBillService
    {
        Task<List<RecurringBill>> GetActive(long userId);
        Task<RecurringBill> Create(RecurringBill recurringBill);
        Task<RecurringBill?> Update(RecurringBill recurringBill);
        Task<bool> Delete(long id, long userId);
    }
}
