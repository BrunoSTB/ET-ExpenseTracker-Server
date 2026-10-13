namespace ExpenseTracker.API.Dtos
{
    public class BillMonthDto
    {
        public int Month { get; set; }
        public List<BillDto> Bills { get; set; }

        public BillMonthDto(int month, List<BillDto> bills)
        {
            Month = month;
            Bills = bills;
        }
    }
}
