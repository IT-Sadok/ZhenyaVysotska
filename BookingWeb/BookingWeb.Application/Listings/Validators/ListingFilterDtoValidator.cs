using BookingWeb.Application.Listings.Requests;
using FluentValidation;

namespace BookingWeb.Application.Listings.Validators;

public sealed class ListingFilterDtoValidator : AbstractValidator<ListingFilterDto>
{
    public ListingFilterDtoValidator()
    {
        RuleFor(x => x.Guests)
            .GreaterThanOrEqualTo(1)
            .When(x => x.Guests is not null);

        RuleFor(x => x.City).MaximumLength(100);
        
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}