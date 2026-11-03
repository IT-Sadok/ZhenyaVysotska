using BookingWeb.Application.Listings.Requests;

namespace BookingWeb.Application.Listings;

public sealed record ListingSearchCriteria(
    int? Guests,
    decimal? MinPrice,
    decimal? MaxPrice,
    BoundingBox? Area,
    StayPeriod? Stay = null)
{
    public static ListingSearchCriteria From(IListingSearchQuery query) =>
        new(query.Guests, query.MinPrice, query.MaxPrice, BoundingBox.From(query), StayPeriod.From(query));
}

public sealed record BoundingBox(double MinLat, double MinLng, double MaxLat, double MaxLng)
{
    public static BoundingBox? From(IListingSearchQuery query) =>
        query is { MinLat: { } minLat, MinLng: { } minLng, MaxLat: { } maxLat, MaxLng: { } maxLng }
            ? new BoundingBox(minLat, minLng, maxLat, maxLng)
            : null;
}

/// <summary>Requested stay; listings with a confirmed booking overlapping it are excluded.</summary>
public sealed record StayPeriod(DateOnly CheckIn, DateOnly CheckOut)
{
    public static StayPeriod? From(IListingSearchQuery query) =>
        query is { CheckIn: { } checkIn, CheckOut: { } checkOut }
            ? new StayPeriod(checkIn, checkOut)
            : null;
}
