namespace ExpenseTracker.API.Controllers.RequestModels
{
    public class LoginRequestModel
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public LoginRequestModel(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}
