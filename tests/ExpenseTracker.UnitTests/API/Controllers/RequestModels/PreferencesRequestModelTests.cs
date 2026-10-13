using ExpenseTracker.API.Controllers.RequestModels;
using ExpenseTracker.UnitTests.TestHelpers;

namespace ExpenseTracker.UnitTests.API.Controllers.RequestModels;

public class PreferencesRequestModelTests
{
    [Theory]
    [InlineData("BRL", "pt-BR")]
    [InlineData("USD", "en-US")]
    [InlineData("EUR", "de-DE")]
    [InlineData("JPY", "ja")]
    public void Validate_WithKnownCurrencyAndLocale_ReturnsNoErrors(string currency, string locale)
    {
        // Arrange
        var model = new PreferencesRequestModel { Currency = currency, Locale = locale };

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData("XYZ")]
    [InlineData("brl")]
    [InlineData("BR")]
    [InlineData("REAL")]
    [InlineData("")]
    public void Validate_WithInvalidCurrency_ReturnsCurrencyError(string currency)
    {
        // Arrange
        var model = new PreferencesRequestModel { Currency = currency, Locale = "pt-BR" };

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(PreferencesRequestModel.Currency));
    }

    [Theory]
    [InlineData("xx-INVALID")]
    [InlineData("not a culture")]
    [InlineData("")]
    public void Validate_WithInvalidLocale_ReturnsLocaleError(string locale)
    {
        // Arrange
        var model = new PreferencesRequestModel { Currency = "BRL", Locale = locale };

        // Act
        var results = ModelValidation.Validate(model);

        // Assert
        results.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(PreferencesRequestModel.Locale));
    }

    [Theory]
    [InlineData("pt-br", "pt-BR")]
    [InlineData("EN-us", "en-US")]
    public void FindCulture_WithDifferentCasing_ReturnsCanonicalName(string locale, string expected)
    {
        // Act
        var culture = PreferencesRequestModel.FindCulture(locale);

        // Assert
        culture!.Name.Should().Be(expected);
    }
}
