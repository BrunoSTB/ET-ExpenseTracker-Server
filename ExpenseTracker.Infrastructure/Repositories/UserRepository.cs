using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Domain.Models;
using ExpenseTracker.Infrastructure.DbConfiguration;
using ExpenseTracker.Infrastructure.DbConfiguration.DataModels;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        public PsqlDbContext Context { get; }

        public UserRepository(PsqlDbContext context)
        {
            Context = context;
        }

        public async Task<User?> GetById(long id)
        {
            var userDataModel = await Context.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (userDataModel == null)
            {
                return null;
            }

            return new User(userDataModel.Username, userDataModel.Password)
            {
                Id = userDataModel.Id,
                Email = userDataModel.Email
            };
        }

        public async Task<User?> CreateUser(User user)
        {
            var userDataModel = new UserDataModel(user.Username, user.Password) { Email = user.Email };

            var result = await Context.Users.AddAsync(userDataModel);
            Context.SaveChanges();

            return new User(result.Entity.Username, result.Entity.Password);
        }

        public async Task<User?> GetByUsername(string username)
        {
            var userDataModel = await Context.Users.FirstOrDefaultAsync(x => x.Username == username);

            if (userDataModel == null)
            {
                return null;
            }

            return new User(userDataModel.Username, userDataModel.Password) { Id = userDataModel.Id };
        }
    }
}
