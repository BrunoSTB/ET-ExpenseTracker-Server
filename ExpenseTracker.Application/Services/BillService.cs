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

            var bills = await _billRepository.GetByYear(year, userId);
            return await WithEstimates(bills, userId);
        }

        public async Task<BillOverview> Create(Bill bill)
        {
            bill.RecurringBillId = null;
            bill.PaidAt = null;

            var created = await _billRepository.Create(bill);
            return new BillOverview(created, null);
        }

        public async Task<bool> Delete(long id, long userId)
        {
            var bill = await _billRepository.GetById(id, userId);
            if (bill == null)
            {
                return false;
            }

            if (bill.RecurringBillId != null)
            {
                return await _billRepository.Skip(id, Today(), userId);
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

            bill.PaidAt = Today();
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

        private DateOnly Today()
        {
            return DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        }

        private async Task<BillOverview?> Save(Bill bill)
        {
            if (!await _billRepository.Update(bill))
            {
                return null;
            }

            var overviews = await WithEstimates([bill], bill.UserId);
            return overviews[0];
        }

        private async Task<List<BillOverview>> WithEstimates(List<Bill> bills, long userId)
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
                .Select(bill => new BillOverview(bill, EstimateFor(bill, lastPaidValues)))
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
