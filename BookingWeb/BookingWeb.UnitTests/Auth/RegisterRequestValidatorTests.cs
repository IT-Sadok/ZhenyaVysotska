using BookingWeb.Application.Auth.Requests;
using BookingWeb.Application.Auth.Validators;
using BookingWeb.Domain;
using FluentValidation.TestHelper;

namespace BookingWeb.UnitTests.Auth;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();
    
    private static RegisterRequest CreateValidRequest()
    {
        return new RegisterRequest("user@example.com", "Passw0rd!", "Ivan", "Petrenko", Roles.Client);
    }

    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        var result = _validator.TestValidate(CreateValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    public void Validate_ShouldFail_WhenEmailIsInvalid(string email)
    {
        var result = _validator.TestValidate(CreateValidRequest() with { Email = email });

        result.ShouldHaveValidationErrorFor(request => request.Email);
    }

    [Theory]
    [InlineData("Short1")]       
    [InlineData("alllower1")]    
    [InlineData("ALLUPPER1")]   
    [InlineData("NoDigitsHere")] 
    public void Validate_ShouldFail_WhenPasswordIsWeak(string password)
    {
        var result = _validator.TestValidate(CreateValidRequest() with { Password = password });

        result.ShouldHaveValidationErrorFor(request => request.Password);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenFirstNameIsBlank(string firstName)
    {
        var result = _validator.TestValidate(CreateValidRequest() with { FirstName = firstName });

        result.ShouldHaveValidationErrorFor(request => request.FirstName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenLastNameIsBlank(string lastName)
    {
        var result = _validator.TestValidate(CreateValidRequest() with { LastName = lastName });

        result.ShouldHaveValidationErrorFor(request => request.LastName);
    }

    [Fact]
    public void Validate_ShouldFail_WhenFirstNameIsLongerThan128Characters()
    {
        var result = _validator.TestValidate(CreateValidRequest() with { FirstName = new string('a', 129) });

        result.ShouldHaveValidationErrorFor(request => request.FirstName);
    }

    [Fact]
    public void Validate_ShouldPass_WhenFirstNameIsExactly128Characters()
    {
        var result = _validator.TestValidate(CreateValidRequest() with { FirstName = new string('a', 128) });

        result.ShouldNotHaveValidationErrorFor(request => request.FirstName);
    }

    [Theory]
    [InlineData(Roles.Client)]
    [InlineData(Roles.Host)]
    public void Validate_ShouldPass_WhenRoleIsSelfAssignable(string role)
    {
        var result = _validator.TestValidate(CreateValidRequest() with { Role = role });

        result.ShouldNotHaveValidationErrorFor(request => request.Role);
    }

    [Theory]
    [InlineData(Roles.Admin)] 
    [InlineData("Superuser")]
    [InlineData("")]
    public void Validate_ShouldFail_WhenRoleIsNotSelfAssignable(string role)
    {
        var result = _validator.TestValidate(CreateValidRequest() with { Role = role });

        result.ShouldHaveValidationErrorFor(request => request.Role);
    }
}
 