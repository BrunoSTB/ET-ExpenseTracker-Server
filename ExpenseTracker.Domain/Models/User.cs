namespace ExpenseTracker.Domain.Models
{
    public class User
    {
        public const string DefaultCurrency = "BRL";
        public const string DefaultLocale = "pt-BR";

        public long Id { get; set; }
        public string Username { get; set; }
        public string? Password { get; set; }
        public string? Email { get; set; }
        public string Currency { get; set; } = DefaultCurrency;
        public string Locale { get; set; } = DefaultLocale;

        public User(string username)
        {
            Username = username;
        }
        public User(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}
