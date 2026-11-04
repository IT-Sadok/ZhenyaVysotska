namespace BookingWeb.Application.Listings.Requests;

public sealed record MapSearchRequest(
    double? MinLat,
    double? MinLng,
    double? MaxLat,
    double? MaxLng,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int? Guests = null,
    int Limit = MapSearchRequest.MaxLimit) : IListingSearchQuery
{
    public const int MaxLimit = 200;
}
