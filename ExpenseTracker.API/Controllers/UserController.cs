using ExpenseTracker.API.Controllers.RequestModels;
using ExpenseTracker.API.Helpers;
using ExpenseTracker.Application.Services.UserService;
using ExpenseTracker.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {

        private readonly ILogger<UserController> _logger;
        private readonly IUserService _userService;

        public UserController(ILogger<UserController> logger,
                              IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<User>> Get(int id)
        {
            var result = await _userService.GetUserById(id);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpPost]
        [Route("Register")]
        public async Task<ActionResult<User>> Create([FromBody] CreateUserRequestModel requestBody)
        {
            var user = new User(requestBody.Username, requestBody.Password)
            {
                Email = requestBody.Email
            };
            return await _userService.CreateUser(user);
        }

        [HttpPost]
        [Route("Login")]
        public async Task<ActionResult<string>> Login([FromBody] LoginRequestModel requestBody)
        {
            var user = new User(requestBody.Username, requestBody.Password);
            var isExpectedUser = await _userService.Login(user);

            if (!isExpectedUser)
            {
                return BadRequest("Incorrect username or password");
            }

            return Ok(AuthHelpers.GenerateJWTToken(user));
        }
    }
}
