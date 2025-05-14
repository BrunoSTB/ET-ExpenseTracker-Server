
using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetById(int id);
        Task<User?> GetByUsername(string username);
        Task<User?> CreateUser(User user);
    }
}
