using ExpenseTracker.Domain.Models;
using ExpenseTracker.Infrastructure.DbConfiguration.DataModels;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.DbConfiguration
{
    public class PostgresDbContext : DbContext
    {
        public PostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options) { }

        public DbSet<UserDataModel> Users { get; set; }
        public DbSet<RecurringBillDataModel> RecurringBills { get; set; }
        public DbSet<BillDataModel> Bills { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserDataModel>(user =>
            {
                user.HasIndex(u => u.Username).IsUnique();
                user.Property(u => u.Currency).HasMaxLength(3).HasDefaultValue(User.DefaultCurrency);
                user.Property(u => u.Locale).HasMaxLength(35).HasDefaultValue(User.DefaultLocale);
            });

            modelBuilder.Entity<RecurringBillDataModel>(recurringBill =>
            {
                recurringBill.Property(r => r.AmountType).HasConversion<string>().HasMaxLength(32);
                recurringBill.Property(r => r.FixedValue).HasPrecision(18, 2);
                recurringBill.HasIndex(r => r.UserId);
                recurringBill.ToTable(t => t.HasCheckConstraint("CK_RecurringBills_DueDay", "\"DueDay\" BETWEEN 1 AND 31"));
            });

            modelBuilder.Entity<BillDataModel>(bill =>
            {
                bill.Property(b => b.AmountType).HasConversion<string>().HasMaxLength(32);
                bill.Property(b => b.Value).HasPrecision(18, 2);
                bill.HasOne(b => b.RecurringBill)
                    .WithMany()
                    .HasForeignKey(b => b.RecurringBillId)
                    .OnDelete(DeleteBehavior.NoAction);
                bill.HasIndex(b => new { b.RecurringBillId, b.Period }).IsUnique();
                bill.HasIndex(b => new { b.UserId, b.Period });
            });
        }
    }
}
