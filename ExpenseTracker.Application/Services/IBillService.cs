using ExpenseTracker.Application.Models;
using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.Application.Services
{
    public interface IBillService
    {
        Task<List<BillOverview>> GetYear(int year, long userId);
        Task<BillOverview> Create(Bill bill);
        Task<bool> Delete(long id, long userId);
        Task<BillOverview?> Pay(long id, long userId, decimal? value);
        Task<BillOverview?> Unpay(long id, long userId);
    }
}
