using System.ComponentModel.DataAnnotations;
using ExpenseTracker.API.Controllers.RequestModels;
using ExpenseTracker.API.Dtos;
using ExpenseTracker.API.Helpers;
using ExpenseTracker.Application.Exceptions;
using ExpenseTracker.Application.Models;
using ExpenseTracker.Application.Services;
using ExpenseTracker.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class BillController : ControllerBase
    {
        private readonly IBillService _billService;

        public BillController(IBillService billService)
        {
            _billService = billService;
        }

        [HttpGet]
        public async Task<ActionResult<List<BillMonthDto>>> GetYear([FromQuery, BindRequired, Range(BillRequestModel.MinYear, BillRequestModel.MaxYear)] int year)
        {
            var bills = await _billService.GetYear(year, User.GetUserId());

            var months = Enumerable.Range(1, 12)
                .Select(month => new BillMonthDto(month, bills
                    .Where(b => b.Bill.DueDate.Month == month)
                    .Select(b => new BillDto(b))
                    .ToList()))
                .ToList();

            return Ok(months);
        }

        [HttpPost]
        public async Task<ActionResult<BillDto>> Create([FromBody] BillRequestModel requestBody)
        {
            var bill = new Bill(User.GetUserId(), requestBody.Name, requestBody.AmountType!.Value, requestBody.DueDate!.Value)
            {
                Value = requestBody.Value
            };

            var result = await _billService.Create(bill);
            return StatusCode(StatusCodes.Status201Created, new BillDto(result));
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            bool deleted;
            try
            {
                deleted = await _billService.Delete(id, User.GetUserId());
            }
            catch (RecurringBillOwnedException)
            {
                return Problem(statusCode: StatusCodes.Status409Conflict, title: "Bills of a recurring bill are removed through the recurring bill.");
            }

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPut("{id:long}/Payment")]
        public async Task<ActionResult<BillDto>> Pay(long id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] PaymentRequestModel? requestBody)
        {
            BillOverview? result;
            try
            {
                result = await _billService.Pay(id, User.GetUserId(), requestBody?.Value);
            }
            catch (BillValueRequiredException)
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Value is required to pay a bill with a variable amount.");
            }

            if (result == null)
            {
                return NotFound();
            }

            return Ok(new BillDto(result));
        }

        [HttpDelete("{id:long}/Payment")]
        public async Task<ActionResult<BillDto>> Unpay(long id)
        {
            var result = await _billService.Unpay(id, User.GetUserId());
            if (result == null)
            {
                return NotFound();
            }

            return Ok(new BillDto(result));
        }
    }
}
