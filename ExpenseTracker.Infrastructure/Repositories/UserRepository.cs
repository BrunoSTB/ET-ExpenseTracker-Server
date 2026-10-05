using ExpenseTracker.Application.Exceptions;
using ExpenseTracker.Application.IRepositories;
using ExpenseTracker.Domain.Models;
using ExpenseTracker.Infrastructure.DbConfiguration;
using ExpenseTracker.Infrastructure.DbConfiguration.DataModels;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExpenseTracker.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        public PostgresDbContext Context { get; }

        public UserRepository(PostgresDbContext context)
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

        public async Task<User> CreateUser(User user)
        {
            try
            {
                var userDataModel = new UserDataModel(user.Username, user.Password!) { Email = user.Email };

                var result = await Context.Users.AddAsync(userDataModel);
                await Context.SaveChangesAsync();

                return new User(result.Entity.Username, result.Entity.Password)
                {
                    Id = result.Entity.Id,
                    Email = result.Entity.Email
                };
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                throw new UsernameAlreadyTakenException(user.Username, ex);
            }
        }

        public async Task<User?> GetByUsername(string username)
        {
            var userDataModel = await Context.Users.FirstOrDefaultAsync(x => x.Username == username);

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
    }
}
