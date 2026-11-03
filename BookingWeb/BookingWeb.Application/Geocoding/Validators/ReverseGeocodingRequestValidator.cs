using BookingWeb.Application.Geocoding.Requests;
using BookingWeb.Domain.ValueObjects;
using FluentValidation;

namespace BookingWeb.Application.Geocoding.Validators;

public sealed class ReverseGeocodingRequestValidator : AbstractValidator<ReverseGeocodingRequest>
{
    public ReverseGeocodingRequestValidator()
    {
        RuleFor(x => x.Lat).NotNull().InclusiveBetween(GeoPoint.MinLatitude, GeoPoint.MaxLatitude);
        RuleFor(x => x.Lng).NotNull().InclusiveBetween(GeoPoint.MinLongitude, GeoPoint.MaxLongitude);
    }
}
