using ExpenseTracker.API.Controllers.RequestModels;
using ExpenseTracker.Application.Services.ExpenseService;
using ExpenseTracker.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class ExpenseController : ControllerBase
    {

        private readonly ILogger<ExpenseController> _logger;
        private readonly IExpenseService _expenseService;

        public ExpenseController(ILogger<ExpenseController> logger,
                              IExpenseService expenseService)
        {
            _logger = logger;
            _expenseService = expenseService;
        }

        [HttpGet]
        public async Task<ActionResult<List<MonthlyExpenses>>> GetExpensesByYear([FromQuery] int year)
        {
            var result = await _expenseService.GetExpensesByYear(year, GetCurrentUserId());

            if (result == null) 
                return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<Expense>> Create([FromBody] CreateExpenseRequestModel requestModel)
        {
            var expense = new Expense(requestModel.Value, 
                                      requestModel.Name, 
                                      requestModel.Date, 
                                      GetCurrentUserId());

            var result = await _expenseService.CreateExpense(expense);

            if (result == null)
                return NotFound();
            return Ok(result);
        }

        private long GetCurrentUserId()
        {
            return long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        }
    }
}
