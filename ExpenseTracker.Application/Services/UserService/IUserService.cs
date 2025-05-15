using ExpenseTracker.Domain.Dtos;
using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.Application.Services.UserService
{
    public interface IUserService
    {
        Task<User?> GetUserById(long id);
        Task<User?> CreateUser(User user);
        Task<User?> Login(User user);
    }
}
