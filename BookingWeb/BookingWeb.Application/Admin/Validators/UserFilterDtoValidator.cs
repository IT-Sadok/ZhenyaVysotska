using BookingWeb.Application.Constants;
using BookingWeb.Application.Models;
using BookingWeb.Domain;
using FluentValidation;

namespace BookingWeb.Application.Admin.Validators;

public sealed class UserFilterDtoValidator : AbstractValidator<UserFilterDto>
{
    public UserFilterDtoValidator()
    {
        RuleFor(x => x.Role)
            .Must(IsRole)
            .When(x => !string.IsNullOrWhiteSpace(x.Role))   
            .WithMessage($"The role should be one of: {string.Join(", ", Roles.All)}.");

        RuleFor(x => x.SortBy)
            .Must(SortTokens.All.Contains)
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy))   
            .WithMessage($"SortBy should be one of: {string.Join(", ", SortTokens.All)}.");

        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(100); 
    }

    private static bool IsRole(string? roleName)
    {
        var exactRoleName = Roles.GetExactRoleName(roleName);
        return exactRoleName is not null;
    }
}