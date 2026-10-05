using ExpenseTracker.API.Controllers.RequestModels;
using ExpenseTracker.API.Helpers;
using ExpenseTracker.Application.Services;
using ExpenseTracker.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

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
        public async Task<ActionResult<List<MonthlyExpenses>>> GetExpensesByYear([FromQuery, BindRequired, Range(1900, 2200)] int year)
        {
            var result = await _expenseService.GetExpensesByYear(year, User.GetUserId());
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<Expense>> Create([FromBody] CreateExpenseRequestModel requestModel)
        {
            var expense = new Expense(requestModel.Value, 
                                      requestModel.Name, 
                                      requestModel.ExpenseDate!.Value, 
                                      User.GetUserId());

            var result = await _expenseService.CreateExpense(expense);
            return StatusCode(StatusCodes.Status201Created, result);
        }

        [HttpDelete("DeleteByIds")]
        public async Task<IActionResult> DeleteByIds([FromQuery(Name = "ids"), Required, MinLength(1)] long[] ids)
        {
            var userId = User.GetUserId();
            var deletedCount = await _expenseService.DeleteByIds(ids, userId);
            if (deletedCount == 0)
            {
                _logger.LogInformation("No expenses deleted for user {UserId}; none of the ids {Ids} were found", userId, ids);
                return Problem(statusCode: StatusCodes.Status404NotFound, title: "No expenses were found for the given ids.");
            }
            return NoContent();
        }
    }
}
