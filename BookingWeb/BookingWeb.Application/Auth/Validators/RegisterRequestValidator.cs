using BookingWeb.Application.Auth.Requests;
using BookingWeb.Domain;
using FluentValidation;

namespace BookingWeb.Application.Auth.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public  RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must have at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain an lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit");
        
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("Firstname is required.")
            .MaximumLength(128)
            .WithMessage("Firstname must not exceed 128 characters.");
        
        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Lastname is required.")
            .MaximumLength(128)
            .WithMessage("Lastname must not exceed 128 characters.");
        
        RuleFor(x => x.Role)
            .NotEmpty()
            .WithMessage("Role is required.")
            .Must(BeSelfAssignableRole)
            .WithMessage($"Role can only be {Roles.Host} or {Roles.Client}.");
    }
    private static bool BeSelfAssignableRole(string role)
    {
        var exactRoleName = Roles.GetExactRoleName(role);
        return exactRoleName is not null && Roles.SelfAssignable.Contains(exactRoleName);
    }
}