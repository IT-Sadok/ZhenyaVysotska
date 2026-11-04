using BookingWeb.Application.Listings.Requests;
using FluentValidation;

namespace BookingWeb.Application.Listings.Validators;

public sealed class MapSearchRequestValidator : AbstractValidator<MapSearchRequest>
{
    public MapSearchRequestValidator(TimeProvider timeProvider)
    {
        Include(new ListingSearchQueryRules(timeProvider));

        var code = ListingErrors.InvalidBoundingBox.Code;
        RuleFor(x => x.MinLat).NotNull().WithErrorCode(code);
        RuleFor(x => x.MinLng).NotNull().WithErrorCode(code);
        RuleFor(x => x.MaxLat).NotNull().WithErrorCode(code);
        RuleFor(x => x.MaxLng).NotNull().WithErrorCode(code);

        RuleFor(x => x.Limit).InclusiveBetween(1, MapSearchRequest.MaxLimit);
    }
}
