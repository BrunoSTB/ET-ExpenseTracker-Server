using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.API.Controllers.RequestModels
{
    public class CreateUserRequestModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [MinLength(8)]
        public string Password { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public CreateUserRequestModel(string username, string password, string email)
        {
            Username = username;
            Password = password;
            Email = email;
        }
    }
}
