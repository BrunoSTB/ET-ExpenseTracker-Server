using System.ComponentModel.DataAnnotations;
using ExpenseTracker.API.Dtos;
using ExpenseTracker.API.Helpers;
using ExpenseTracker.Application.Services;
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
        public async Task<ActionResult<List<BillMonthDto>>> GetYear([FromQuery, BindRequired, Range(2000, 2999)] int year)
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
    }
}
