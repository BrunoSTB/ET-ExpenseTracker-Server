
using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetById(long id);
        Task<User?> GetByUsername(string username);
        Task<User?> CreateUser(User user);
    }
}
