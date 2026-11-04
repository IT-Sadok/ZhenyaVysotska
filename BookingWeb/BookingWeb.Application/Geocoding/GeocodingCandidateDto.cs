namespace BookingWeb.Application.Geocoding;

public sealed record GeocodingCandidateDto(
    string DisplayName,
    double Latitude,
    double Longitude,
    GeocodingBoundingBoxDto BoundingBox);

public sealed record GeocodingBoundingBoxDto(double MinLat, double MinLng, double MaxLat, double MaxLng);
