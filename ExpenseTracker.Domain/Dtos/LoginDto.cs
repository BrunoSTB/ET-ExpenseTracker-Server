namespace ExpenseTracker.Domain.Dtos
{
    public class LoginDto
    {
        public string Username { get; set; }
        public string AccessToken { get; set; }

        public LoginDto(string username, string accessToken)
        {
            Username = username;
            AccessToken = accessToken;
        }
    }
}
