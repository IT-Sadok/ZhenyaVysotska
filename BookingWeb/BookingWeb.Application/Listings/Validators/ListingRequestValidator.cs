using BookingWeb.Application.Listings.Requests;
using BookingWeb.Domain.ValueObjects;
using FluentValidation;

namespace BookingWeb.Application.Listings.Validators;

public sealed class ListingRequestValidator : AbstractValidator<ListingRequest>
{
    public ListingRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Region).MaximumLength(100);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);

        RuleFor(x => x.Latitude)
            .NotNull()
            .WithErrorCode(ListingErrors.LocationRequired.Code)
            .InclusiveBetween(GeoPoint.MinLatitude, GeoPoint.MaxLatitude);
        RuleFor(x => x.Longitude)
            .NotNull()
            .WithErrorCode(ListingErrors.LocationRequired.Code)
            .InclusiveBetween(GeoPoint.MinLongitude, GeoPoint.MaxLongitude);

        RuleFor(x => x.PricePerNight)
            .GreaterThan(0)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true);

        RuleFor(x => x.MaxGuests).InclusiveBetween(1, 50);
    }
}
