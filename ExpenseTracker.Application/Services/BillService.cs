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

        public BillService(IBillRepository billRepository, IRecurringBillRepository recurringBillRepository)
        {
            _billRepository = billRepository;
            _recurringBillRepository = recurringBillRepository;
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
