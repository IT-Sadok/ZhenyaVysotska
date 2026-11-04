namespace BookingWeb.Application.Geocoding;

public sealed record ReverseGeocodingDto(
    string DisplayName,
    string? City,
    string? Region,
    string? CountryCode);
