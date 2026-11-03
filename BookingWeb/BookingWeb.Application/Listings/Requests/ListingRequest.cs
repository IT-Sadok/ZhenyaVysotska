namespace BookingWeb.Application.Listings.Requests;

public sealed record ListingRequest(
    string Title,
    string? Description,
    string CountryCode,
    string? Region,
    string City,
    string Address,
    double? Latitude,
    double? Longitude,
    decimal PricePerNight,
    int MaxGuests);
