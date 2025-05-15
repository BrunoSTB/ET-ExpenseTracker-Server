using ExpenseTracker.API.Controllers.RequestModels;
using ExpenseTracker.Application.Services.ExpenseService;
using ExpenseTracker.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
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

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Expense>> Create([FromBody] CreateExpenseRequestModel requestModel)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var expense = new Expense(requestModel.Value, requestModel.Name, requestModel.Date, long.Parse(userId!));
            var result = await _expenseService.CreateExpense(expense);

            if (result == null)
                return NotFound();
            return Ok(result);
        }
    }
}
