using ExpenseTracker.Application.Models;

namespace ExpenseTracker.Application.Services
{
    public interface IBillService
    {
        Task<List<BillOverview>> GetYear(int year, long userId);
    }
}
