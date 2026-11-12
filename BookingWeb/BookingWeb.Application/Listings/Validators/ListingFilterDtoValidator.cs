using BookingWeb.Application.Listings.Requests;
using FluentValidation;

namespace BookingWeb.Application.Listings.Validators;

public sealed class ListingFilterDtoValidator : AbstractValidator<ListingFilterDto>
{
    public ListingFilterDtoValidator(TimeProvider timeProvider)
    {
        Include(new ListingSearchQueryRules(timeProvider));

        RuleFor(x => x)
            .Must(HasAllOrNoBoundingBoxValues)
            .WithErrorCode(ListingErrors.InvalidBoundingBox.Code)
            .WithMessage("Pass all four of minLat, minLng, maxLat, maxLng, or none of them.")
            .OverridePropertyName(ListingSearchQueryRules.BoundingBoxProperty);

        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }

    private static bool HasAllOrNoBoundingBoxValues(ListingFilterDto filter)
    {
        var present = new[] { filter.MinLat, filter.MinLng, filter.MaxLat, filter.MaxLng }
            .Count(v => v is not null);
        return present is 0 or 4;
    }
}
