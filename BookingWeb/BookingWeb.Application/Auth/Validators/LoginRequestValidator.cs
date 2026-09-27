using BookingWeb.Application.Auth.Requests;
using FluentValidation;

namespace BookingWeb.Application.Auth.Validators;

public sealed class LoginRequestValidator: AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}