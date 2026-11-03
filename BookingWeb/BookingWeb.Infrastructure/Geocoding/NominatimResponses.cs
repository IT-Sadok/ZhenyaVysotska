using System.Text.Json.Serialization;

namespace BookingWeb.Infrastructure.Geocoding; 

internal sealed record NominatimPlace(
    [property: JsonPropertyName("lat")] string Lat,
    [property: JsonPropertyName("lon")] string Lon,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("boundingbox")] string[] BoundingBox);

internal sealed record NominatimReverseResponse(
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("display_name")] string? DisplayName,
    [property: JsonPropertyName("address")] Dictionary<string, string>? Address);
