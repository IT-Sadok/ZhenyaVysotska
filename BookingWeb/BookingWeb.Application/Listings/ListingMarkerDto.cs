namespace BookingWeb.Application.Listings;

public sealed record ListingMarkerDto(
    Guid Id,
    string Title,
    decimal PricePerNight,
    double Latitude,
    double Longitude);

public sealed record MapMarkersDto(IReadOnlyList<ListingMarkerDto> Markers, bool IsTruncated);
