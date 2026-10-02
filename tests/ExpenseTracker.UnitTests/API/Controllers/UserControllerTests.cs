using System.Security.Claims;
using System.Text.Json;
using ExpenseTracker.API.Controllers;
using ExpenseTracker.API.Controllers.RequestModels;
using ExpenseTracker.Application.Exceptions;
using ExpenseTracker.Application.Services;
using ExpenseTracker.API.Dtos;
using ExpenseTracker.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ExpenseTracker.UnitTests.API.Controllers;

public class UserControllerTests
{
    private const long CurrentUserId = 42;

    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly UserController _controller;

    public UserControllerTests()
    {
        _controller = new UserController(NullLogger<UserController>.Instance, _userService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, CurrentUserId.ToString())], "Test"))
                }
            }
        };
    }

    [Fact]
    public async Task Me_WhenUserExists_ReturnsCurrentUserWithoutPassword()
    {
        // Arrange
        _userService.GetUserById(CurrentUserId)
            .Returns(new User("bruno", "hashed-password") { Id = CurrentUserId, Email = "bruno@example.com" });

        // Act
        var result = await _controller.Me();

        // Assert
        var dto = result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<UserDto>().Subject;
        dto.Id.Should().Be(CurrentUserId);
        dto.Username.Should().Be("bruno");
        dto.Email.Should().Be("bruno@example.com");
        JsonSerializer.Serialize(dto).Should().NotContainEquivalentOf("password");
    }

    [Fact]
    public async Task Me_WithAuthenticatedUser_QueriesUserIdFromToken()
    {
        // Act
        await _controller.Me();

        // Assert
        await _userService.Received(1).GetUserById(CurrentUserId);
        await _userService.DidNotReceive().GetUserById(Arg.Is<long>(id => id != CurrentUserId));
    }

    [Fact]
    public async Task Me_WhenUserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        _userService.GetUserById(CurrentUserId).Returns((User?)null);

        // Act
        var result = await _controller.Me();

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_WhenRegistrationSucceeds_ReturnsCreatedIdWithoutPassword()
    {
        // Arrange
        _userService.CreateUser(Arg.Any<User>())
            .Returns(call => new User(call.Arg<User>().Username, "hashed-password") { Id = 7, Email = call.Arg<User>().Email });

        // Act
        var result = await _controller.Create(new CreateUserRequestModel("bruno", "S3cret!", "bruno@example.com"));

        // Assert
        var dto = result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<UserDto>().Subject;
        dto.Id.Should().Be(7);
        dto.Username.Should().Be("bruno");
        JsonSerializer.Serialize(dto).Should().NotContainEquivalentOf("password");
    }

    [Fact]
    public async Task Create_WithTakenUsername_ReturnsConflict()
    {
        // Arrange
        _userService.CreateUser(Arg.Any<User>()).ThrowsAsync(new UsernameAlreadyTakenException("bruno"));

        // Act
        var result = await _controller.Create(new CreateUserRequestModel("bruno", "S3cret!", "bruno@example.com"));

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        _userService.CreateUser(Arg.Any<User>()).Returns((User?)null);

        // Act
        var result = await _controller.Create(new CreateUserRequestModel("bruno", "S3cret!", "bruno@example.com"));

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
