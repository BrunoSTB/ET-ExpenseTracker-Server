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
    private const long OtherUserId = 8;
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

    [Fact]
    public async Task GetPreferences_WithToken_ReturnsCurrentUsersCurrencyAndLocale()
    {
        // Arrange
        var storedUser = StoredUser();
        storedUser.Currency = "USD";
        storedUser.Locale = "en-US";
        _factory.UserRepository.GetById(UserId).Returns(storedUser);
        var request = Authorized(HttpMethod.Get, "/User/Preferences", TestTokens.For(_factory, storedUser));

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var preferences = await response.Content.ReadFromJsonAsync<PreferencesDto>(TestContext.Current.CancellationToken);
        preferences!.Currency.Should().Be("USD");
        preferences.Locale.Should().Be("en-US");
        await _factory.UserRepository.DidNotReceive().GetById(OtherUserId);
    }

    [Fact]
    public async Task GetPreferences_WithMissingUser_ReturnsNotFoundProblem()
    {
        // Arrange
        var request = Authorized(HttpMethod.Get, "/User/Preferences", TestTokens.For(_factory, new User(Username) { Id = UserId }));

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
    }

    [Fact]
    public async Task GetPreferences_WithoutToken_ReturnsUnauthorizedProblem()
    {
        // Act
        var response = await _client.GetAsync("/User/Preferences", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
    }

    [Fact]
    public async Task UpdatePreferences_WithValidBody_UpdatesCurrentUserAndReturnsCanonicalLocale()
    {
        // Arrange
        _factory.UserRepository.UpdatePreferences(UserId, "USD", "en-US").Returns(true);
        var request = Authorized(HttpMethod.Put, "/User/Preferences", TestTokens.For(_factory, new User(Username) { Id = UserId }));
        request.Content = JsonContent.Create(new { currency = "USD", locale = "en-us", userId = OtherUserId });

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var preferences = await response.Content.ReadFromJsonAsync<PreferencesDto>(TestContext.Current.CancellationToken);
        preferences!.Currency.Should().Be("USD");
        preferences.Locale.Should().Be("en-US");
        await _factory.UserRepository.Received(1).UpdatePreferences(UserId, "USD", "en-US");
        await _factory.UserRepository.DidNotReceive().UpdatePreferences(OtherUserId, Arg.Any<string>(), Arg.Any<string>());
    }

    [Theory]
    [InlineData("XYZ", "pt-BR")]
    [InlineData("brl", "pt-BR")]
    [InlineData("", "pt-BR")]
    [InlineData(null, "pt-BR")]
    [InlineData("BRL", "xx-INVALID")]
    [InlineData("BRL", "")]
    [InlineData("BRL", null)]
    public async Task UpdatePreferences_WithInvalidBody_ReturnsValidationProblem(string? currency, string? locale)
    {
        // Arrange
        var request = Authorized(HttpMethod.Put, "/User/Preferences", TestTokens.For(_factory, new User(Username) { Id = UserId }));
        request.Content = JsonContent.Create(new { currency, locale });

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
        await _factory.UserRepository.DidNotReceive().UpdatePreferences(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task UpdatePreferences_WithMissingUser_ReturnsNotFoundProblem()
    {
        // Arrange
        _factory.UserRepository.UpdatePreferences(UserId, "BRL", "pt-BR").Returns(false);
        var request = Authorized(HttpMethod.Put, "/User/Preferences", TestTokens.For(_factory, new User(Username) { Id = UserId }));
        request.Content = JsonContent.Create(new { currency = "BRL", locale = "pt-BR" });

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
    }

    [Fact]
    public async Task UpdatePreferences_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PutAsJsonAsync("/User/Preferences", new { currency = "BRL", locale = "pt-BR" },
                                                    TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
