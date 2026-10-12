using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ExpenseTracker.API.Dtos;
using ExpenseTracker.Application.Exceptions;
using ExpenseTracker.Domain.Models;
using ExpenseTracker.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ExpenseTracker.IntegrationTests.Api;

public class UserEndpointsTests : IAsyncDisposable
{
    private const long UserId = 7;
    private const string Username = "bruno";
    private const string Password = "correct-password";
    private const string ProblemJson = "application/problem+json";

    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public UserEndpointsTests()
    {
        _client = _factory.CreateApiClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Register_WithNewUsername_ReturnsCreatedWithoutPassword()
    {
        // Arrange
        _factory.UserRepository.CreateUser(Arg.Any<User>())
            .Returns(call =>
            {
                var user = call.Arg<User>();
                user.Id = UserId;
                return user;
            });
        var body = new { username = Username, password = Password, email = "bruno@example.com" };

        // Act
        var response = await _client.PostAsJsonAsync("/User/Register", body, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        json.Should().NotContainEquivalentOf("password");
        var user = await response.Content.ReadFromJsonAsync<UserDto>(TestContext.Current.CancellationToken);
        user!.Id.Should().Be(UserId);
        user.Username.Should().Be(Username);
    }

    [Fact]
    public async Task Register_WithTakenUsername_ReturnsConflictProblem()
    {
        // Arrange
        _factory.UserRepository.CreateUser(Arg.Any<User>()).ThrowsAsync(new UsernameAlreadyTakenException(Username));
        var body = new { username = Username, password = Password, email = "bruno@example.com" };

        // Act
        var response = await _client.PostAsJsonAsync("/User/Register", body, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorizedProblem()
    {
        // Arrange
        _factory.UserRepository.GetByUsername(Username).Returns(StoredUser());
        var body = new { username = Username, password = "wrong-password" };

        // Act
        var response = await _client.PostAsJsonAsync("/User/Login", body, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsTokenAcceptedByMe()
    {
        // Arrange
        var storedUser = StoredUser();
        _factory.UserRepository.GetByUsername(Username).Returns(storedUser);
        _factory.UserRepository.GetById(UserId).Returns(storedUser);
        var body = new { username = Username, password = Password };

        // Act
        var loginResponse = await _client.PostAsJsonAsync("/User/Login", body, TestContext.Current.CancellationToken);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginDto>(TestContext.Current.CancellationToken);
        var meResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/User/me", login!.AccessToken),
                                                 TestContext.Current.CancellationToken);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        login.Username.Should().Be(Username);
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorizedProblem()
    {
        // Act
        var response = await _client.GetAsync("/User/me", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
    }

    [Fact]
    public async Task Me_WithToken_ReturnsUserWithoutPasswordHash()
    {
        // Arrange
        var storedUser = StoredUser();
        _factory.UserRepository.GetById(UserId).Returns(storedUser);
        var request = Authorized(HttpMethod.Get, "/User/me", TestTokens.For(_factory, storedUser));

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        json.Should().NotContain(storedUser.Password!);
        json.Should().NotContainEquivalentOf("password");
        var user = await response.Content.ReadFromJsonAsync<UserDto>(TestContext.Current.CancellationToken);
        user!.Id.Should().Be(UserId);
        user.Username.Should().Be(Username);
        user.Email.Should().Be(storedUser.Email);
    }

    [Fact]
    public async Task Me_WhenRepositoryThrowsUnexpectedly_ReturnsInternalServerErrorProblem()
    {
        // Arrange
        _factory.UserRepository.GetById(UserId).ThrowsAsync(new InvalidOperationException("database is down"));
        var request = Authorized(HttpMethod.Get, "/User/me", TestTokens.For(_factory, new User(Username) { Id = UserId }));

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        json.Should().NotContain("database is down");
    }

    private static User StoredUser()
    {
        var user = new User(Username) { Id = UserId, Email = "bruno@example.com" };
        user.Password = new PasswordHasher<User>().HashPassword(user, Password);
        return user;
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}
