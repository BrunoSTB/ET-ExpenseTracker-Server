using ExpenseTracker.API.Controllers.RequestModels;
using ExpenseTracker.UnitTests.TestHelpers;

namespace ExpenseTracker.UnitTests.API.Controllers.RequestModels;

public class CreateUserRequestModelTests
{
    [Fact]
    public void Validate_WithValidModel_ReturnsNoErrors()
    {
        // Arrange
        var model = new CreateUserRequestModel("bruno", "S3cret!pass", "bruno@example.com");

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithBlankUsername_ReturnsUsernameError(string? username)
    {
        // Arrange
        var model = new CreateUserRequestModel(username!, "S3cret!pass", "bruno@example.com");

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(CreateUserRequestModel.Username));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("short")]
    public void Validate_WithMissingOrShortPassword_ReturnsPasswordError(string? password)
    {
        // Arrange
        var model = new CreateUserRequestModel("bruno", password!, "bruno@example.com");

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(CreateUserRequestModel.Password));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("not-an-email")]
    public void Validate_WithMissingOrInvalidEmail_ReturnsEmailError(string? email)
    {
        // Arrange
        var model = new CreateUserRequestModel("bruno", "S3cret!pass", email!);

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(CreateUserRequestModel.Email));
    }
}
