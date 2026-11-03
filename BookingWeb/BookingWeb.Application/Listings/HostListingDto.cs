namespace BookingWeb.Application.Listings;

public sealed record HostListingDto(
    Guid Id,
    Guid HostId,
    string Title,
    string? Description,
    string CountryCode,
    string? Region,
    string City,
    string Address,
    double Latitude,
    double Longitude,
    double PublicLatitude,
    double PublicLongitude,
    decimal PricePerNight,
    int MaxGuests,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);
