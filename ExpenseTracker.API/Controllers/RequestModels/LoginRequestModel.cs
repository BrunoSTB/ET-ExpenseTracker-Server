using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.API.Controllers.RequestModels
{
    public class LoginRequestModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        public LoginRequestModel(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}
