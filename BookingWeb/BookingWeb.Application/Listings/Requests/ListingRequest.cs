namespace BookingWeb.Application.Listings.Requests;

public sealed record ListingRequest(
    string Title,
    string? Description,
    string City,
    string Address,
    decimal PricePerNight,
    int MaxGuests);