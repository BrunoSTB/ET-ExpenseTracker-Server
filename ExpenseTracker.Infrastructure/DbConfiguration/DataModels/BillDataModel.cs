using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.Infrastructure.DbConfiguration.DataModels
{
    public class BillDataModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long? RecurringBillId { get; set; }

        #pragma warning disable CS8618 // Non nullable field must have value when exiting constructor, but efcore requires empty constructor to create migrations correctly.
        public UserDataModel User { get; set; }
        public string Name { get; set; }
        #pragma warning restore CS8618 // Non nullable field must have value when exiting constructor, but efcore requires empty constructor to create migrations correctly.

        public RecurringBillDataModel? RecurringBill { get; set; }
        public AmountType AmountType { get; set; }
        public DateOnly DueDate { get; set; }
        public DateOnly Period { get; set; }
        public decimal? Value { get; set; }
        public DateOnly? PaidAt { get; set; }
        public DateOnly? SkippedAt { get; set; }
    }
}
