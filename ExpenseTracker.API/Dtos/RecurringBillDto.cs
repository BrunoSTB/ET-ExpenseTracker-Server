using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.API.Dtos
{
    public class RecurringBillDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public int DueDay { get; set; }
        public AmountType AmountType { get; set; }
        public decimal? FixedValue { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly? EndMonth { get; set; }

        public RecurringBillDto(RecurringBill recurringBill)
        {
            Id = recurringBill.Id;
            Name = recurringBill.Name;
            DueDay = recurringBill.DueDay;
            AmountType = recurringBill.AmountType;
            FixedValue = recurringBill.FixedValue;
            StartMonth = recurringBill.StartMonth;
            EndMonth = recurringBill.EndMonth;
        }
    }
}
