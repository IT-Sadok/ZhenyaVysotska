using BookingWeb.Application.Auth.Requests;
using BookingWeb.Application.Auth.Validators;
using BookingWeb.Domain;
using FluentValidation.TestHelper;

namespace BookingWeb.UnitTests.Auth;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();
    
    private static RegisterRequest Valid() =>
        new("user@example.com", "Passw0rd!", "Ivan", "Paliychuk", Roles.Client);
 
    [Fact]
    public void Valid_request_passes()
    {
        var result = _validator.TestValidate(Valid());
        result.ShouldNotHaveAnyValidationErrors();
    }
 
    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    public void Invalid_email_fails(string email)
    {
        var result = _validator.TestValidate(Valid() with { Email = email });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
 
    [Theory]
    [InlineData("short1A")]     
    [InlineData("alllower1")]   
    [InlineData("ALLUPPER1")]   
    [InlineData("NoDigitsAA")]  
    public void Weak_password_fails(string password)
    {
        var result = _validator.TestValidate(Valid() with { Password = password });
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
 
    [Fact]
    public void Strong_password_passes()
    {
        var result = _validator.TestValidate(Valid() with { Password = "Strong1Pass" });
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
 
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_first_name_fails(string firstName)
    {
        var result = _validator.TestValidate(Valid() with { FirstName = firstName });
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }
 
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_last_name_fails(string lastName)
    {
        var result = _validator.TestValidate(Valid() with { LastName = lastName });
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }
 
    [Fact]
    public void Too_long_first_name_fails()
    {
        var result = _validator.TestValidate(Valid() with { FirstName = new string('a', 129) });
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }
 
    [Fact]
    public void Too_long_last_name_fails()
    {
        var result = _validator.TestValidate(Valid() with { LastName = new string('a', 129) });
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }
 
    [Fact]
    public void First_name_at_max_length_passes()
    {
        var result = _validator.TestValidate(Valid() with { FirstName = new string('a', 128) });
        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
    }
 
    [Fact]
    public void Admin_role_is_rejected()
    {
        var result = _validator.TestValidate(Valid() with { Role = Roles.Admin });
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }
 
    [Fact]
    public void Empty_role_fails()
    {
        var result = _validator.TestValidate(Valid() with { Role = "" });
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }
 
    [Fact]
    public void Unknown_role_fails()
    {
        var result = _validator.TestValidate(Valid() with { Role = "Superuser" });
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }
 
    [Theory]
    [InlineData("Client")]
    [InlineData("Host")]
    public void Switchable_roles_pass(string role)
    {
        var result = _validator.TestValidate(Valid() with { Role = role });
        result.ShouldNotHaveValidationErrorFor(x => x.Role);
    }
}
 