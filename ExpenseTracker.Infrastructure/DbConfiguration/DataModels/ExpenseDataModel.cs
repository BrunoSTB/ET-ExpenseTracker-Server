namespace ExpenseTracker.Infrastructure.DbConfiguration.DataModels
{
    public class ExpenseDataModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        
        #pragma warning disable CS8618 // Non nullable field must have value when exiting constructor, but efcore requires empty constructor to create migrations correctly.
        public UserDataModel User { get; set; }
        public string Name { get; set; }
        #pragma warning restore CS8618 // Non nullable field must have value when exiting constructor, but efcore requires empty constructor to create migrations correctly.

        public decimal Value { get; set; }
        public DateTime ExpenseDate { get; set; }
    }
}
