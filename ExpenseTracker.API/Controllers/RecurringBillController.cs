using ExpenseTracker.API.Controllers.RequestModels;
using ExpenseTracker.API.Dtos;
using ExpenseTracker.API.Helpers;
using ExpenseTracker.Application.Services;
using ExpenseTracker.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class RecurringBillController : ControllerBase
    {
        private readonly IRecurringBillService _recurringBillService;

        public RecurringBillController(IRecurringBillService recurringBillService)
        {
            _recurringBillService = recurringBillService;
        }

        [HttpGet]
        public async Task<ActionResult<List<RecurringBillDto>>> GetActive()
        {
            var result = await _recurringBillService.GetActive(User.GetUserId());
            return Ok(result.Select(r => new RecurringBillDto(r)).ToList());
        }

        [HttpPost]
        public async Task<ActionResult<RecurringBillDto>> Create([FromBody] CreateRecurringBillRequestModel requestBody)
        {
            var recurringBill = ToDomain(requestBody, User.GetUserId());
            recurringBill.StartMonth = requestBody.StartMonth ?? default;

            var result = await _recurringBillService.Create(recurringBill);
            return StatusCode(StatusCodes.Status201Created, new RecurringBillDto(result));
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<RecurringBillDto>> Update(long id, [FromBody] RecurringBillRequestModel requestBody)
        {
            var recurringBill = ToDomain(requestBody, User.GetUserId());
            recurringBill.Id = id;

            var result = await _recurringBillService.Update(recurringBill);
            if (result == null)
            {
                return NotFound();
            }

            return Ok(new RecurringBillDto(result));
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var deleted = await _recurringBillService.Delete(id, User.GetUserId());
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }

        private static RecurringBill ToDomain(RecurringBillRequestModel requestBody, long userId)
        {
            return new RecurringBill(userId, requestBody.Name, requestBody.DueDay, requestBody.AmountType!.Value, default)
            {
                FixedValue = requestBody.FixedValue
            };
        }
    }
}
