namespace ExpenseTracker.Infrastructure.DbConfiguration.DataModels
{
    public class ExpenseDataModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public UserDataModel? User { get; set; }
        public decimal Value { get; set; }
        public string? Name { get; set; }
        public DateTime ExpenseDate { get; set; }
    }
}
