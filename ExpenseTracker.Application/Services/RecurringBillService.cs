using ExpenseTracker.Application.IRepositories;
using ExpenseTracker.Domain.Models;

namespace ExpenseTracker.Application.Services
{
    public class RecurringBillService : IRecurringBillService
    {
        private readonly IRecurringBillRepository _recurringBillRepository;
        private readonly IBillRepository _billRepository;
        private readonly TimeProvider _timeProvider;

        public RecurringBillService(IRecurringBillRepository recurringBillRepository,
                                    IBillRepository billRepository,
                                    TimeProvider timeProvider)
        {
            _recurringBillRepository = recurringBillRepository;
            _billRepository = billRepository;
            _timeProvider = timeProvider;
        }

        public async Task<List<RecurringBill>> GetActive(long userId)
        {
            return await _recurringBillRepository.GetActive(userId, CurrentMonth());
        }

        public async Task<RecurringBill> Create(RecurringBill recurringBill)
        {
            recurringBill.StartMonth = recurringBill.StartMonth == default
                ? CurrentMonth()
                : RecurringBill.MonthOf(recurringBill.StartMonth);
            recurringBill.EndMonth = null;

            return await _recurringBillRepository.Create(recurringBill);
        }

        public async Task<RecurringBill?> Update(RecurringBill recurringBill)
        {
            var currentMonth = CurrentMonth();
            var existing = await _recurringBillRepository.GetById(recurringBill.Id, recurringBill.UserId);
            if (existing == null || existing.IsEndedBy(currentMonth))
            {
                return null;
            }

            existing.Name = recurringBill.Name;
            existing.DueDay = recurringBill.DueDay;
            existing.AmountType = recurringBill.AmountType;
            existing.FixedValue = recurringBill.FixedValue;

            await _recurringBillRepository.Update(existing);
            await _billRepository.UpdateUnpaidFrom(existing, currentMonth);
            return existing;
        }

        public async Task<bool> Delete(long id, long userId)
        {
            var currentMonth = CurrentMonth();
            var existing = await _recurringBillRepository.GetById(id, userId);
            if (existing == null || existing.IsEndedBy(currentMonth))
            {
                return false;
            }

            existing.EndMonth = currentMonth;

            await _recurringBillRepository.Update(existing);
            await _billRepository.DeleteUnpaidFrom(id, currentMonth, userId);
            await _billRepository.DetachFrom(id, currentMonth, userId);
            return true;
        }

        private DateOnly CurrentMonth()
        {
            return RecurringBill.MonthOf(DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime));
        }
    }
}
