using ExpenseTracker.Application.IRepositories;
using ExpenseTracker.Application.Services;
using ExpenseTracker.Domain.Models;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace ExpenseTracker.UnitTests.Application.Services;

public class UserServiceTests
{
    private const string Username = "bruno";
    private const string PlainPassword = "S3cret!";

    private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
    private readonly UserService _service;

    public UserServiceTests()
    {
        _service = new UserService(_repository);
    }

    [Fact]
    public async Task CreateUser_WithPlainPassword_PersistsVerifiableHash()
    {
        // Arrange
        User? persisted = null;
        _repository.CreateUser(Arg.Do<User>(u => persisted = u)).Returns(call => call.Arg<User>());
        var user = new User(Username, PlainPassword);

        // Act
        await _service.CreateUser(user);

        // Assert
        persisted.Should().NotBeNull();
        persisted!.Password.Should().NotBeNullOrEmpty().And.NotBe(PlainPassword);
        new PasswordHasher<User>()
            .VerifyHashedPassword(persisted, persisted.Password!, PlainPassword)
            .Should().NotBe(PasswordVerificationResult.Failed);
    }

    [Fact]
    public async Task Login_WithUnknownUsername_ReturnsNull()
    {
        // Arrange
        _repository.GetByUsername(Username).Returns((User?)null);

        // Act
        var result = await _service.Login(new User(Username, PlainPassword));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsNull()
    {
        // Arrange
        var stored = CreateStoredUser();
        _repository.GetByUsername(Username).Returns(stored);

        // Act
        var result = await _service.Login(new User(Username, "wrong-password"));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsStoredUser()
    {
        // Arrange
        var stored = CreateStoredUser();
        _repository.GetByUsername(Username).Returns(stored);

        // Act
        var result = await _service.Login(new User(Username, PlainPassword));

        // Assert
        result.Should().BeSameAs(stored);
    }

    private static User CreateStoredUser()
    {
        var stored = new User(Username) { Id = 1 };
        stored.Password = new PasswordHasher<User>().HashPassword(stored, PlainPassword);
        return stored;
    }
}
