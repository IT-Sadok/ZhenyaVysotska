using BookingWeb.Application.Auth.Requests;
using BookingWeb.Domain;
using FluentValidation;

namespace BookingWeb.Application.Auth.Validators;

public sealed class AddRoleRequestValidator : AbstractValidator<AddRoleRequest>
{
    public AddRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(BeSelfAssignableRole)
            .WithMessage($"Role must be one of: {string.Join(", ", Roles.SelfAssignable)}.");
    }
 
    private static bool BeSelfAssignableRole(string? role)
    {
        var exactRoleName = Roles.GetExactRoleName(role);
        return exactRoleName is not null && Roles.SelfAssignable.Contains(exactRoleName);
    }
}