using ExpenseTracker.Application.IRepositories;
using ExpenseTracker.Domain.Models;
using ExpenseTracker.Infrastructure.DbConfiguration;
using ExpenseTracker.Infrastructure.DbConfiguration.DataModels;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Repositories
{
    public class BillRepository : IBillRepository
    {
        public PostgresDbContext Context { get; }

        public BillRepository(PostgresDbContext context)
        {
            Context = context;
        }

        public async Task<Bill?> GetById(long id, long userId)
        {
            var dataModel = await Context.Bills
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && x.SkippedAt == null);

            return dataModel == null ? null : ToDomain(dataModel);
        }

        public async Task<List<Bill>> GetByYear(int year, long userId)
        {
            var from = new DateOnly(year, 1, 1);
            var to = from.AddYears(1);

            var dataModels = await Context.Bills
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.Period >= from && x.Period < to && x.SkippedAt == null)
                .OrderBy(x => x.DueDate)
                .ThenBy(x => x.Id)
                .ToListAsync();

            return dataModels.Select(ToDomain).ToList();
        }

        public async Task<Bill> Create(Bill bill)
        {
            var dataModel = new BillDataModel
            {
                UserId = bill.UserId,
                RecurringBillId = bill.RecurringBillId,
                Name = bill.Name,
                AmountType = bill.AmountType,
                DueDate = bill.DueDate,
                Period = PeriodOf(bill.DueDate),
                Value = bill.Value,
                PaidAt = bill.PaidAt,
                SkippedAt = bill.SkippedAt
            };

            await Context.Bills.AddAsync(dataModel);
            await Context.SaveChangesAsync();
            return ToDomain(dataModel);
        }

        public async Task<int> CreateMissing(IEnumerable<Bill> bills)
        {
            await using var transaction = await Context.Database.BeginTransactionAsync();

            var created = 0;
            foreach (var bill in bills)
            {
                created += await Context.Database.ExecuteSqlAsync($"""
                    INSERT INTO "Bills" ("UserId", "RecurringBillId", "Name", "AmountType", "DueDate", "Period", "Value", "PaidAt")
                    VALUES ({bill.UserId}, {bill.RecurringBillId}, {bill.Name}, {bill.AmountType.ToString()}, {bill.DueDate}, {PeriodOf(bill.DueDate)}, {bill.Value}, {bill.PaidAt})
                    ON CONFLICT ("RecurringBillId", "Period") DO NOTHING
                    """);
            }

            await transaction.CommitAsync();
            return created;
        }

        public async Task<bool> Update(Bill bill)
        {
            var updated = await Context.Bills
                .Where(x => x.Id == bill.Id && x.UserId == bill.UserId && x.SkippedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Name, bill.Name)
                    .SetProperty(x => x.AmountType, bill.AmountType)
                    .SetProperty(x => x.DueDate, bill.DueDate)
                    .SetProperty(x => x.Period, PeriodOf(bill.DueDate))
                    .SetProperty(x => x.Value, bill.Value)
                    .SetProperty(x => x.PaidAt, bill.PaidAt));

            return updated > 0;
        }

        public async Task<bool> Delete(long id, long userId)
        {
            var deleted = await Context.Bills
                .Where(x => x.Id == id && x.UserId == userId)
                .ExecuteDeleteAsync();

            return deleted > 0;
        }

        public async Task<bool> Skip(long id, DateOnly skippedAt, long userId)
        {
            var skipped = await Context.Bills
                .Where(x => x.Id == id && x.UserId == userId && x.SkippedAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.SkippedAt, skippedAt));

            return skipped > 0;
        }

        public async Task<Dictionary<long, List<decimal>>> GetLastPaidValues(IReadOnlyCollection<long> recurringBillIds, int count, long userId)
        {
            var paid = await Context.Bills
                .AsNoTracking()
                .Where(x => x.UserId == userId &&
                            x.RecurringBillId != null &&
                            recurringBillIds.Contains(x.RecurringBillId.Value) &&
                            x.PaidAt != null &&
                            x.Value != null &&
                            x.SkippedAt == null)
                .OrderByDescending(x => x.Period)
                .Select(x => new { RecurringBillId = x.RecurringBillId!.Value, Value = x.Value!.Value })
                .ToListAsync();

            return paid
                .GroupBy(x => x.RecurringBillId)
                .ToDictionary(g => g.Key, g => g.Take(count).Select(x => x.Value).ToList());
        }

        public async Task<int> UpdateUnpaidFrom(RecurringBill recurringBill, DateOnly fromMonth)
        {
            var from = PeriodOf(fromMonth);

            return await Context.Database.ExecuteSqlAsync($"""
                UPDATE "Bills"
                SET "Name" = {recurringBill.Name},
                    "AmountType" = {recurringBill.AmountType.ToString()},
                    "Value" = {recurringBill.BillValue},
                    "DueDate" = LEAST("Period" + ({recurringBill.DueDay} - 1), ("Period" + INTERVAL '1 month' - INTERVAL '1 day')::date)
                WHERE "RecurringBillId" = {recurringBill.Id}
                  AND "UserId" = {recurringBill.UserId}
                  AND "PaidAt" IS NULL
                  AND "SkippedAt" IS NULL
                  AND "Period" >= {from}
                """);
        }

        public async Task<int> DeleteUnpaidFrom(long recurringBillId, DateOnly fromMonth, long userId)
        {
            var from = PeriodOf(fromMonth);

            return await Context.Bills
                .Where(x => x.RecurringBillId == recurringBillId &&
                            x.UserId == userId &&
                            (x.PaidAt == null || x.SkippedAt != null) &&
                            x.Period >= from)
                .ExecuteDeleteAsync();
        }

        private static DateOnly PeriodOf(DateOnly date)
        {
            return new DateOnly(date.Year, date.Month, 1);
        }

        private static Bill ToDomain(BillDataModel dataModel)
        {
            return new Bill(dataModel.UserId, dataModel.Name, dataModel.AmountType, dataModel.DueDate)
            {
                Id = dataModel.Id,
                RecurringBillId = dataModel.RecurringBillId,
                Value = dataModel.Value,
                PaidAt = dataModel.PaidAt,
                SkippedAt = dataModel.SkippedAt
            };
        }
    }
}
