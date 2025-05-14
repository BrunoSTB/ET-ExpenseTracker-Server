using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.Application.Services.UserService
{
    public interface IUserService
    {
        Task<User?> GetUserById(int id);
        Task<User> CreateUser(User user);
        Task<bool> Login(User user);
    }
}
