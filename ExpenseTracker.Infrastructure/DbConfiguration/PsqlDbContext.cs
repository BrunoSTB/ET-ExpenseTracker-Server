using ExpenseTracker.Infrastructure.DbConfiguration.DataModels;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.DbConfiguration
{
    public class PsqlDbContext : DbContext
    {
        public PsqlDbContext(DbContextOptions<PsqlDbContext> options) : base(options) { }

        public DbSet<UserDataModel> Users { get; set; }
        public DbSet<MonthlyExpensesDataModel> MonthlyExpenses { get; set; }
        public DbSet<ExpenseDataModel> Expenses { get; set; }
    }
}
