namespace ExpenseTracker.Domain.Models
{
    public class RecurringBill
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Name { get; set; }
        public int DueDay { get; set; }
        public AmountType AmountType { get; set; }
        public decimal? FixedValue { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly? EndMonth { get; set; }

        public RecurringBill(long userId, string name, int dueDay, AmountType amountType, DateOnly startMonth)
        {
            UserId = userId;
            Name = name;
            DueDay = dueDay;
            AmountType = amountType;
            StartMonth = startMonth;
        }

        public decimal? BillValue => AmountType == AmountType.Fixed ? FixedValue : null;

        public bool IsActiveIn(DateOnly month)
        {
            var period = MonthOf(month);
            return period >= StartMonth && !IsEndedBy(period);
        }

        public bool IsEndedBy(DateOnly month)
        {
            return EndMonth != null && EndMonth <= MonthOf(month);
        }

        public DateOnly DueDateIn(DateOnly month)
        {
            var day = Math.Min(DueDay, DateTime.DaysInMonth(month.Year, month.Month));
            return new DateOnly(month.Year, month.Month, day);
        }

        public Bill CreateBillFor(DateOnly month)
        {
            return new Bill(UserId, Name, AmountType, DueDateIn(month))
            {
                RecurringBillId = Id,
                Value = BillValue
            };
        }

        public static DateOnly MonthOf(DateOnly date)
        {
            return new DateOnly(date.Year, date.Month, 1);
        }
    }
}
