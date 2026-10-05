using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.Application.Services
{
    public interface IUserService
    {
        Task<User?> GetUserById(long id);
        Task<User> CreateUser(User user);
        Task<User?> Login(User user);
    }
}
