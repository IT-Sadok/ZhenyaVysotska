using BookingWeb.Application.Auth.Requests;
using BookingWeb.Application.Auth.Validators;
using BookingWeb.Domain;
using FluentValidation.TestHelper;

namespace BookingWeb.UnitTests.Auth;

public class AddRoleRequestValidatorTests
{
    private readonly AddRoleRequestValidator _validator = new();
    
    [Theory]
    [InlineData("Client")]
    [InlineData("Host")]
    [InlineData("host")] 
    [InlineData("CLIENT")]
    public void Validate_ShouldPass_WhenRoleIsSelfAssignableInAnyCase(string role)
    {
        var result = _validator.TestValidate(new AddRoleRequest(role));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData("admin")]
    public void Validate_ShouldFail_WhenRoleIsAdmin(string role)
    {
        var result = _validator.TestValidate(new AddRoleRequest(role));

        result.ShouldHaveValidationErrorFor(request => request.Role);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Superuser")]
    public void Validate_ShouldFail_WhenRoleIsUnknownOrEmpty(string role)
    {
        var result = _validator.TestValidate(new AddRoleRequest(role));

        result.ShouldHaveValidationErrorFor(request => request.Role);
    }

}