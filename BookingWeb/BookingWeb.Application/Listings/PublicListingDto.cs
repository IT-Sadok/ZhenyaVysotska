namespace BookingWeb.Application.Listings;

public sealed record PublicListingDto(
    Guid Id,
    Guid HostId,
    string Title,
    string? Description,
    string CountryCode,
    string? Region,
    string City,
    double Latitude,
    double Longitude,
    string TimeZoneId,
    decimal PricePerNight,
    int MaxGuests);
