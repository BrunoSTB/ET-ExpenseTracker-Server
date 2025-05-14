namespace ExpenseTracker.API.Controllers.RequestModels
{
    public class CreateUserRequestModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }

        public CreateUserRequestModel(string username, string password, string email)
        {
            Username = username;
            Password = password;
            Email = email;
        }
    }
}
