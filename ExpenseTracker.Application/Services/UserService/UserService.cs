using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Domain.Models;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

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
            var hasher = new PasswordHasher<User>();
            user.Password = hasher.HashPassword(user, user.Password!);
            var result = await _userRepository.CreateUser(user);
            return result;
        }

        public async Task<User?> Login(User userInput)
        {
            var expectedUser = await _userRepository.GetByUsername(userInput.Username);
            var hasher = new PasswordHasher<User>();

            if (expectedUser == null)
            {
                return null;
            }

            var result = hasher.VerifyHashedPassword(expectedUser, expectedUser.Password!, userInput.Password!);
            if (result == PasswordVerificationResult.Failed)
            {
                return null;
            }

            return expectedUser;
        }
    }
}
