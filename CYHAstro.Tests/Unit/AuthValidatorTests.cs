using CYHAstro.Core.DTOs.Auth;
using CYHAstro.Core.Validators;
using FluentAssertions;
using Xunit;

namespace CYHAstro.Tests.Unit;

public class AuthValidatorTests
{
    private readonly RegisterRequestValidator _registerValidator = new();
    private readonly LoginRequestValidator _loginValidator = new();
    private readonly RefreshTokenRequestValidator _refreshValidator = new();

    [Theory]
    [InlineData("valid@example.com", "SecurePass123!", "Rahul Sharma", "+919876543210", true)]
    [InlineData("invalid-email", "SecurePass123!", "Rahul", "+919876543210", false)]
    [InlineData("valid@example.com", "weak", "Rahul", "+919876543210", false)] // short password
    [InlineData("valid@example.com", "alllowercase123!", "Rahul", "+919876543210", false)] // no uppercase
    [InlineData("valid@example.com", "ALLUPPERCASE123!", "Rahul", "+919876543210", false)] // no lowercase
    [InlineData("valid@example.com", "NoDigitsHere!", "Rahul", "+919876543210", false)] // no digit
    [InlineData("valid@example.com", "NoSpecial123", "Rahul", "+919876543210", false)] // no special char
    [InlineData("valid@example.com", "SecurePass123!", "", "+919876543210", false)] // empty display name
    [InlineData("valid@example.com", "SecurePass123!", "Rahul", "invalid-phone", false)] // invalid phone
    public void RegisterValidator_ShouldValidateCorrectly(
        string email, string password, string displayName, string? phone, bool expectedValid)
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Email = email,
            Password = password,
            DisplayName = displayName,
            PhoneNumber = phone
        };

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData("user@example.com", "Password123!", true)]
    [InlineData("not-an-email", "Password123!", false)]
    [InlineData("user@example.com", "", false)]
    [InlineData("", "Password123!", false)]
    public void LoginValidator_ShouldValidateCorrectly(string email, string password, bool expectedValid)
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = email,
            Password = password
        };

        // Act
        var result = _loginValidator.Validate(request);

        // Assert
        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData("valid_access_token", "valid_refresh_token", true)]
    [InlineData("", "valid_refresh_token", false)]
    [InlineData("valid_access_token", "", false)]
    public void RefreshTokenValidator_ShouldValidateCorrectly(string accessToken, string refreshToken, bool expectedValid)
    {
        // Arrange
        var request = new RefreshTokenRequestDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };

        // Act
        var result = _refreshValidator.Validate(request);

        // Assert
        result.IsValid.Should().Be(expectedValid);
    }
}
