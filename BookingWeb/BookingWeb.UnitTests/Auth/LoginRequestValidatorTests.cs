using BookingWeb.Application.Auth.Requests;
using BookingWeb.Application.Auth.Validators;
using FluentValidation.TestHelper;

namespace BookingWeb.UnitTests.Auth;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Valid_login_passes()
    {
        var result = _validator.TestValidate(new LoginRequest("user@example.com", "whatever"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Invalid_email_fails(string email)
    {
        var result = _validator.TestValidate(new LoginRequest(email, "whatever"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Empty_password_fails()
    {
        var result = _validator.TestValidate(new LoginRequest("user@example.com", ""));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}