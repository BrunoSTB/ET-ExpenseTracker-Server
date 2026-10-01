using ExpenseTracker.API.Controllers.RequestModels;
using ExpenseTracker.API.Dtos;
using ExpenseTracker.API.Helpers;
using ExpenseTracker.Application.Exceptions;
using ExpenseTracker.Application.Services;
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
        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> Me()
        {
            var result = await _userService.GetUserById(User.GetUserId());

            if (result == null)
            {
                return NotFound();
            }

            return Ok(ToDto(result));
        }

        [HttpPost]
        [Route("Register")]
        public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequestModel requestBody)
        {
            var user = new User(requestBody.Username, requestBody.Password)
            {
                Email = requestBody.Email
            };

            User? result;
            try
            {
                result = await _userService.CreateUser(user);
            }
            catch (UsernameAlreadyTakenException)
            {
                return Conflict("Username already taken.");
            }

            if (result == null)
            {
                return BadRequest("Registration failed.");
            }

            return Ok(ToDto(result));
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

        private static UserDto ToDto(User user)
        {
            return new UserDto(user.Id, user.Username, user.Email);
        }
    }
}
