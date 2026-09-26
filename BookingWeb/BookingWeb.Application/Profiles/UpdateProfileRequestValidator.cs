using BookingWeb.Application.Profiles.Requests;
using FluentValidation;

namespace BookingWeb.Application.Profiles;

public sealed class UpdateProfileRequestValidator:AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Bio).MaximumLength(1000);
        RuleFor(x => x.AvatarUrl).MaximumLength(500);
    }
}