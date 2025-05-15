using ExpenseTracker.API.Controllers.RequestModels;
using ExpenseTracker.API.Helpers;
using ExpenseTracker.Application.Services.UserService;
using ExpenseTracker.Domain.Dtos;
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
            var result = await _userService.CreateUser(user);

            if (result == null)
            {
                return BadRequest("Registration failed.");
            }

            return Ok(result);
        }

        [HttpPost]
        [Route("Login")]
        public async Task<ActionResult<LoginDto>> Login([FromBody] LoginRequestModel requestBody)
        {
            var userRequest = new User(requestBody.Username, requestBody.Password);
            var expectedUser = await _userService.Login(userRequest);

            if (expectedUser == null)
            {
                return BadRequest("Incorrect username or password");
            }
            var result = new LoginDto(expectedUser.Username, AuthHelpers.GenerateJWTToken(expectedUser));
            return Ok(result);
        }
    }
}
