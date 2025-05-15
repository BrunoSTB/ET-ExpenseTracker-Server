namespace ExpenseTracker.API.Controllers.RequestModels
{
    public class CreateExpenseRequestModel
    {
        public string Name { get; set; }
        public decimal Value { get; set; }
        public DateTime Date { get; set; }

        public CreateExpenseRequestModel(string name, decimal value, DateTime date)
        {
            Name = name;
            Value = value;
            Date = date;
        }
    }
}
