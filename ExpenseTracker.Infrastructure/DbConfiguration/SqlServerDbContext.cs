using ExpenseTracker.Infrastructure.DbConfiguration.DataModels;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.DbConfiguration
{
    public class SqlServerDbContext : DbContext
    {
        public SqlServerDbContext(DbContextOptions<SqlServerDbContext> options) : base(options) { }

        public DbSet<UserDataModel> Users { get; set; }
        public DbSet<ExpenseDataModel> Expenses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ensure decimal precision/scale is configured to avoid silent truncation in SQL Server
            modelBuilder.Entity<ExpenseDataModel>()
                .Property(e => e.Value)
                .HasPrecision(18, 2);
        }
    }
}
