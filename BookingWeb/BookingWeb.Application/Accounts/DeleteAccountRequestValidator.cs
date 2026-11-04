using FluentValidation;

namespace BookingWeb.Application.Accounts;

public sealed class DeleteAccountRequestValidator : AbstractValidator<DeleteAccountRequest>
{
    public DeleteAccountRequestValidator()
    {
        RuleFor(request => request.Password)
            .NotEmpty()
            .WithMessage("Password is required");
    }
}