namespace BookingWeb.Application.Listings.Requests;

public sealed record ListingFilterDto(
    string? City,
    int? Guests,
    int Page = 1,
    int PageSize = 20);