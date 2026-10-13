using ExpenseTracker.Application.Exceptions;
using ExpenseTracker.Application.IRepositories;
using ExpenseTracker.Application.Models;
using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.Application.Services
{
    public class BillService : IBillService
    {
        public const int EstimateSampleSize = 3;

        private readonly IBillRepository _billRepository;
        private readonly IRecurringBillRepository _recurringBillRepository;
        private readonly TimeProvider _timeProvider;

        public BillService(IBillRepository billRepository,
                           IRecurringBillRepository recurringBillRepository,
                           TimeProvider timeProvider)
        {
            _billRepository = billRepository;
            _recurringBillRepository = recurringBillRepository;
            _timeProvider = timeProvider;
        }

        public async Task<List<BillOverview>> GetYear(int year, long userId)
        {
            var firstMonth = new DateOnly(year, 1, 1);
            var months = Enumerable.Range(0, 12).Select(firstMonth.AddMonths).ToList();

            var recurringBills = await _recurringBillRepository.GetActive(userId, firstMonth);
            var missing = recurringBills
                .SelectMany(recurringBill => months
                    .Where(recurringBill.IsActiveIn)
                    .Select(recurringBill.CreateBillFor))
                .ToList();

            if (missing.Count > 0)
            {
                await _billRepository.CreateMissing(missing);
            }

            // A template missing from the active list ended before the year, so
            // it no longer owns any of the year's bills.
            var templates = recurringBills.ToDictionary(r => r.Id);
            var bills = await _billRepository.GetByYear(year, userId);
            return await WithEstimates(bills, userId, bill =>
                bill.RecurringBillId == null ||
                !templates.TryGetValue(bill.RecurringBillId.Value, out var template) ||
                template.IsEndedBy(bill.DueDate));
        }

        public async Task<BillOverview> Create(Bill bill)
        {
            bill.RecurringBillId = null;
            bill.PaidAt = null;

            var created = await _billRepository.Create(bill);
            return new BillOverview(created, null, CanDelete: true);
        }

        public async Task<bool> Delete(long id, long userId)
        {
            var bill = await _billRepository.GetById(id, userId);
            if (bill == null)
            {
                return false;
            }

            if (!await CanDelete(bill))
            {
                throw new RecurringBillOwnedException(id);
            }

            return await _billRepository.Delete(id, userId);
        }

        public async Task<BillOverview?> Pay(long id, long userId, decimal? value)
        {
            var bill = await _billRepository.GetById(id, userId);
            if (bill == null)
            {
                return null;
            }

            if (value == null && bill.AmountType != AmountType.Fixed)
            {
                throw new BillValueRequiredException(id);
            }

            bill.PaidAt = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
            bill.Value = value ?? bill.Value;

            return await Save(bill);
        }

        public async Task<BillOverview?> Unpay(long id, long userId)
        {
            var bill = await _billRepository.GetById(id, userId);
            if (bill == null)
            {
                return null;
            }

            bill.PaidAt = null;
            if (bill.AmountType != AmountType.Fixed)
            {
                bill.Value = null;
            }

            return await Save(bill);
        }

        private async Task<BillOverview?> Save(Bill bill)
        {
            if (!await _billRepository.Update(bill))
            {
                return null;
            }

            var canDelete = await CanDelete(bill);
            var overviews = await WithEstimates([bill], bill.UserId, _ => canDelete);
            return overviews[0];
        }

        // While its template is active in the bill's month, the template owns the
        // bill (and would generate it again), so it is removed through the
        // template. Once the template ended by that month, the bill stands alone.
        private async Task<bool> CanDelete(Bill bill)
        {
            if (bill.RecurringBillId == null)
            {
                return true;
            }

            var recurringBill = await _recurringBillRepository.GetById(bill.RecurringBillId.Value, bill.UserId);
            return recurringBill != null && recurringBill.IsEndedBy(bill.DueDate);
        }

        private async Task<List<BillOverview>> WithEstimates(List<Bill> bills, long userId, Func<Bill, bool> canDelete)
        {
            var estimatedIds = bills
                .Where(b => b.AmountType == AmountType.VariableEstimated && b.RecurringBillId != null)
                .Select(b => b.RecurringBillId!.Value)
                .Distinct()
                .ToList();

            var lastPaidValues = estimatedIds.Count > 0
                ? await _billRepository.GetLastPaidValues(estimatedIds, EstimateSampleSize, userId)
                : new Dictionary<long, List<decimal>>();

            return bills
                .Select(bill => new BillOverview(bill, EstimateFor(bill, lastPaidValues), canDelete(bill)))
                .ToList();
        }

        private static decimal? EstimateFor(Bill bill, Dictionary<long, List<decimal>> lastPaidValues)
        {
            if (bill.AmountType != AmountType.VariableEstimated ||
                bill.RecurringBillId == null ||
                !lastPaidValues.TryGetValue(bill.RecurringBillId.Value, out var values) ||
                values.Count == 0)
            {
                return null;
            }

            return Math.Round(values.Average(), 2);
        }
    }
}
