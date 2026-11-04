using BookingWeb.Application.Common;
using BookingWeb.Application.Listings.Requests;
using BookingWeb.Domain.ValueObjects;
using FluentValidation;

namespace BookingWeb.Application.Listings.Validators;

internal sealed class ListingSearchQueryRules : AbstractValidator<IListingSearchQuery>
{
    public const double MaxSpanDegrees = 10;

    public const string BoundingBoxProperty = "BoundingBox";

    public ListingSearchQueryRules(TimeProvider timeProvider)
    {
        RuleFor(x => x.Guests).GreaterThanOrEqualTo(1);
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(x => x.MinPrice)
            .When(x => x.MinPrice is not null && x.MaxPrice is not null)
            .WithMessage("Max price must be greater than or equal to min price.");

        var code = ListingErrors.InvalidBoundingBox.Code;
        
        RuleFor(x => x.MinLat).InclusiveBetween(GeoPoint.MinLatitude, GeoPoint.MaxLatitude).WithErrorCode(code);
        RuleFor(x => x.MaxLat).InclusiveBetween(GeoPoint.MinLatitude, GeoPoint.MaxLatitude).WithErrorCode(code);
        RuleFor(x => x.MinLng).InclusiveBetween(GeoPoint.MinLongitude, GeoPoint.MaxLongitude).WithErrorCode(code);
        RuleFor(x => x.MaxLng).InclusiveBetween(GeoPoint.MinLongitude, GeoPoint.MaxLongitude).WithErrorCode(code);

        RuleFor(x => BoundingBox.From(x))
            .Must(box => box!.MinLat < box.MaxLat && box.MinLng < box.MaxLng)
            .WithErrorCode(code)
            .WithMessage("Min values must be less than max values. Boxes crossing the 180th meridian are not supported.")
            .Must(box => box!.MaxLat - box.MinLat <= MaxSpanDegrees 
                         && box.MaxLng - box.MinLng <= MaxSpanDegrees)
            .WithErrorCode(code)
            .WithMessage($"Bounding box must not exceed {MaxSpanDegrees} degrees in latitude or longitude.")
            .When(x => BoundingBox.From(x) is not null)
            .OverridePropertyName(BoundingBoxProperty);

        RuleFor(x => x.CheckIn)
            .NotNull()
            .When(x => x.CheckOut is not null)
            .WithMessage("CheckIn is required when CheckOut is given.");

        RuleFor(x => x.CheckOut)
            .NotNull()
            .When(x => x.CheckIn is not null)
            .WithMessage("CheckOut is required when CheckIn is given.");

        RuleFor(x => x.CheckIn)
            .Must(checkIn => checkIn >= timeProvider.GetEarliestDateOnEarth())
            .When(x => x.CheckIn is not null)
            .WithMessage("CheckIn can't be in the past.");

        RuleFor(x => x.CheckOut)
            .Must((query, checkOut) => checkOut > query.CheckIn)
            .When(x => x.CheckIn is not null && x.CheckOut is not null)
            .WithMessage("CheckOut must be after CheckIn.");
    }
}
