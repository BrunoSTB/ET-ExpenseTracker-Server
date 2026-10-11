using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.API.Services
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}
