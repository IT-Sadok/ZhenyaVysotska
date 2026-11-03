namespace BookingWeb.Application.Listings.Requests;

public sealed record ListingFilterDto(
    int? Guests = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    double? MinLat = null,
    double? MinLng = null,
    double? MaxLat = null,
    double? MaxLng = null,
    int Page = 1,
    int PageSize = 20) : IListingSearchQuery;
