using ExpenseTracker.Application.Models;
using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.API.Dtos
{
    public class BillDto
    {
        public long Id { get; set; }
        public long? RecurringBillId { get; set; }
        public string Name { get; set; }
        public DateOnly DueDate { get; set; }
        public AmountType AmountType { get; set; }
        public decimal? Value { get; set; }
        public decimal? Estimate { get; set; }
        public DateOnly? PaidAt { get; set; }
        public bool CanDelete { get; set; }

        public BillDto(BillOverview overview)
        {
            Id = overview.Bill.Id;
            RecurringBillId = overview.Bill.RecurringBillId;
            Name = overview.Bill.Name;
            DueDate = overview.Bill.DueDate;
            AmountType = overview.Bill.AmountType;
            Value = overview.Bill.Value;
            Estimate = overview.Estimate;
            PaidAt = overview.Bill.PaidAt;
            CanDelete = overview.CanDelete;
        }
    }
}
