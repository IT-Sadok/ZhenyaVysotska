using BookingWeb.Application.Listings.Requests;

namespace BookingWeb.Application.Listings;

public sealed record ListingSearchCriteria(
    int? Guests,
    decimal? MinPrice,
    decimal? MaxPrice,
    BoundingBox? Area)
{
    public static ListingSearchCriteria From(IListingSearchQuery query) =>
        new(query.Guests, query.MinPrice, query.MaxPrice, BoundingBox.From(query));
}

public sealed record BoundingBox(double MinLat, double MinLng, double MaxLat, double MaxLng)
{
    public static BoundingBox? From(IListingSearchQuery query) =>
        query is { MinLat: { } minLat, MinLng: { } minLng, MaxLat: { } maxLat, MaxLng: { } maxLng }
            ? new BoundingBox(minLat, minLng, maxLat, maxLng)
            : null;
}
