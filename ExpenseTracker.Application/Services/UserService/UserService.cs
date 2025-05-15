using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.Application.Services.UserService
{
    public class UserService : IUserService
    {
        private IUserRepository _userRepository { get; set; }

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<User?> GetUserById(long id)
        {
            return await _userRepository.GetById(id);
        }

        public async Task<User?> CreateUser(User user)
        {
            var result = await _userRepository.CreateUser(user);
            return result;
        }

        public async Task<User?> Login(User userInput)
        {
            var expectedUser = await _userRepository.GetByUsername(userInput.Username);

            if (expectedUser == null || expectedUser.Password != userInput.Password)
            {
                return null;
            }

            return expectedUser;
        }
    }
}
