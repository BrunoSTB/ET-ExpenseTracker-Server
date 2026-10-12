using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.Application.IRepositories
{
    public interface IBillRepository
    {
        Task<Bill?> GetById(long id, long userId);
        Task<List<Bill>> GetByYear(int year, long userId);
        Task<Bill> Create(Bill bill);
        Task<int> CreateMissing(IEnumerable<Bill> bills);
        Task<bool> Update(Bill bill);
        Task<bool> Delete(long id, long userId);
        Task<int> DeleteUnpaidFrom(long recurringBillId, DateOnly fromMonth, long userId);
    }
}
