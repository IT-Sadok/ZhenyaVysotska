using BookingWeb.Application.Common;
using BookingWeb.Application.Listings.Requests;
using FluentValidation;

namespace BookingWeb.Application.Listings.Validators;

public sealed class ListingFilterDtoValidator : AbstractValidator<ListingFilterDto>
{
    public ListingFilterDtoValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.CheckIn)
            .NotNull()
            .When(x => x.CheckOut is not null)
            .WithMessage("CheckIn is required when CheckOut is given.");

        RuleFor(x => x.CheckOut)
            .NotNull()
            .When(x => x.CheckIn is not null)
            .WithMessage("CheckOut is required when CheckIn is given.");
        
        RuleFor(x => x.CheckIn)
            .Must(checkIn => checkIn >= timeProvider.GetUtcToday())
            .When(x => x.CheckIn is not null)
            .WithMessage("CheckIn can't be in the past.");

        RuleFor(x => x.CheckOut)
            .Must((filter, checkOut) => checkOut > filter.CheckIn)
            .When(x => x.CheckIn is not null && x.CheckOut is not null)
            .WithMessage("CheckOut must be after CheckIn.");
        
        RuleFor(x => x.Guests)
            .GreaterThanOrEqualTo(1)
            .When(x => x.Guests is not null);

        RuleFor(x => x.City).MaximumLength(100);
        
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}