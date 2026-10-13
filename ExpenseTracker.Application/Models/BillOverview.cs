using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.Application.Models
{
    public record BillOverview(Bill Bill, decimal? Estimate);
}
