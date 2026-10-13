using ExpenseTracker.Application.IRepositories;
using ExpenseTracker.Domain.Models;
using ExpenseTracker.Infrastructure.DbConfiguration;
using ExpenseTracker.Infrastructure.DbConfiguration.DataModels;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Repositories
{
    public class RecurringBillRepository : IRecurringBillRepository
    {
        public PostgresDbContext Context { get; }

        public RecurringBillRepository(PostgresDbContext context)
        {
            Context = context;
        }

        public async Task<RecurringBill?> GetById(long id, long userId)
        {
            var dataModel = await Context.RecurringBills
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            return dataModel == null ? null : ToDomain(dataModel);
        }

        public async Task<List<RecurringBill>> GetActive(long userId, DateOnly fromMonth)
        {
            var dataModels = await Context.RecurringBills
                .AsNoTracking()
                .Where(x => x.UserId == userId && (x.EndMonth == null || x.EndMonth >= fromMonth))
                .OrderBy(x => x.Id)
                .ToListAsync();

            return dataModels.Select(ToDomain).ToList();
        }

        public async Task<RecurringBill> Create(RecurringBill recurringBill)
        {
            var dataModel = new RecurringBillDataModel
            {
                UserId = recurringBill.UserId,
                Name = recurringBill.Name,
                DueDay = recurringBill.DueDay,
                AmountType = recurringBill.AmountType,
                FixedValue = recurringBill.FixedValue,
                StartMonth = recurringBill.StartMonth,
                EndMonth = recurringBill.EndMonth
            };

            await Context.RecurringBills.AddAsync(dataModel);
            await Context.SaveChangesAsync();
            return ToDomain(dataModel);
        }

        public async Task<bool> Update(RecurringBill recurringBill)
        {
            var updated = await Context.RecurringBills
                .Where(x => x.Id == recurringBill.Id && x.UserId == recurringBill.UserId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Name, recurringBill.Name)
                    .SetProperty(x => x.DueDay, recurringBill.DueDay)
                    .SetProperty(x => x.AmountType, recurringBill.AmountType)
                    .SetProperty(x => x.FixedValue, recurringBill.FixedValue)
                    .SetProperty(x => x.StartMonth, recurringBill.StartMonth)
                    .SetProperty(x => x.EndMonth, recurringBill.EndMonth));

            return updated > 0;
        }

        private static RecurringBill ToDomain(RecurringBillDataModel dataModel)
        {
            return new RecurringBill(dataModel.UserId, dataModel.Name, dataModel.DueDay, dataModel.AmountType, dataModel.StartMonth)
            {
                Id = dataModel.Id,
                FixedValue = dataModel.FixedValue,
                EndMonth = dataModel.EndMonth
            };
        }
    }
}
