using ExpenseTracker.Application.Services.ExpenseService;
using ExpenseTracker.Domain.Models;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet]
        public async Task<ActionResult<Expense>> Create(int id)
        {
            return await _expenseService.GetExpenseById(id);
        }
    }
}
