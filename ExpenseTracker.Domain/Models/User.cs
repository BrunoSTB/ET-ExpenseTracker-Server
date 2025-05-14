namespace ExpenseTracker.Domain.Models
{
    public class User
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string? Email { get; set; }
        public List<MonthlyExpenses> MonthlyExpenses { get; set; }

        public User(string username, string password)
        {
            Username = username;
            Password = password;
            MonthlyExpenses = new List<MonthlyExpenses>();
        }
    }
}
