using BookingWeb.Application.Auth.Requests;
using BookingWeb.Application.Auth.Validators;
using FluentValidation.TestHelper;

namespace BookingWeb.UnitTests.Auth;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        var result = _validator.TestValidate(new LoginRequest("user@example.com", "any-password"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_ShouldFail_WhenEmailIsInvalid(string email)
    {
        var result = _validator.TestValidate(new LoginRequest(email, "any-password"));

        result.ShouldHaveValidationErrorFor(request => request.Email);
    }

    [Fact]
    public void Validate_ShouldFail_WhenPasswordIsEmpty()
    {
        var result = _validator.TestValidate(new LoginRequest("user@example.com", ""));

        result.ShouldHaveValidationErrorFor(request => request.Password);
    }

}