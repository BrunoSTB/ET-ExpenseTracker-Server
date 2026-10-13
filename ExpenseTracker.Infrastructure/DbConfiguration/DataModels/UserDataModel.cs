using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.Infrastructure.DbConfiguration.DataModels
{
    public class UserDataModel
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string? Email { get; set; }
        public string Currency { get; set; } = User.DefaultCurrency;
        public string Locale { get; set; } = User.DefaultLocale;

        public UserDataModel(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}
