using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.Application.Models
{
    // CanDelete: whether DELETE Bill/{id} accepts the bill (standalone, or its
    // template ended by the bill's month).
    public record BillOverview(Bill Bill, decimal? Estimate, bool CanDelete);
}
