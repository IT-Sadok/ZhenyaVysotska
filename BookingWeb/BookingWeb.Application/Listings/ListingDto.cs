namespace BookingWeb.Application.Listings;

public sealed record ListingDto(
    Guid Id,
    Guid HostId,
    string Title,
    string? Description,
    string City,
    string Address,
    decimal PricePerNight,
    int MaxGuests,
    bool IsActive);