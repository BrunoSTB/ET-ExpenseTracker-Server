using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Domain.Models;
using ExpenseTracker.Infrastructure.DbConfiguration;
using ExpenseTracker.Infrastructure.DbConfiguration.DataModels;

namespace ExpenseTracker.Infrastructure.Repositories
{
    public class ExpenseRepository : IExpenseRepository
    {
        public PsqlDbContext Context { get; }
        public IUserRepository UserRepository { get; }

        public ExpenseRepository(PsqlDbContext context, IUserRepository userRepository)
        {
            Context = context;
            UserRepository = userRepository;
        }

        public async Task<Expense?> GetExpenseAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<Expense?> CreateExpense(Expense expense)
        {
            try
            {
                var newEntry = new ExpenseDataModel() 
                { 
                    ExpenseDate = expense.ExpenseDate, 
                    Name =  expense.Name,
                    Value = expense.Value,
                    User = Context.Users.FirstOrDefault(x => x.Id == expense.UserId),
                    UserId = expense.UserId
                };

                var result = await Context.Expenses.AddAsync(newEntry);
                Context.SaveChanges();
                return new Expense(result.Entity.Value,
                                   result.Entity.Name!,
                                   result.Entity.ExpenseDate,
                                   result.Entity.UserId)
                       { 
                            Id = result.Entity.Id
                       };
            }
            catch (Exception ex) 
            {
                Console.WriteLine("Exception while trying to create new Expense. exception: " + ex.Message);
                return null;
            }
        }
    }
}
