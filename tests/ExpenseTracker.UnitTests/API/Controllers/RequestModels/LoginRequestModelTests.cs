using ExpenseTracker.API.Controllers.RequestModels;
using ExpenseTracker.UnitTests.TestHelpers;

namespace ExpenseTracker.UnitTests.API.Controllers.RequestModels;

public class LoginRequestModelTests
{
    [Fact]
    public void Validate_WithValidModel_ReturnsNoErrors()
    {
        // Arrange
        var model = new LoginRequestModel("bruno", "S3cret!pass");

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithBlankUsername_ReturnsUsernameError(string? username)
    {
        // Arrange
        var model = new LoginRequestModel(username!, "S3cret!pass");

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(LoginRequestModel.Username));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithBlankPassword_ReturnsPasswordError(string? password)
    {
        // Arrange
        var model = new LoginRequestModel("bruno", password!);

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(LoginRequestModel.Password));
    }
}
