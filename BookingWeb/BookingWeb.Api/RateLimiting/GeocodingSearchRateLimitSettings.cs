namespace BookingWeb.Api.RateLimiting;

public sealed class GeocodingSearchRateLimitSettings
{
    public const string SectionName = "RateLimiting:GeocodingSearch";
    
    public int PermitLimit { get; init; } = 10;

    public int WindowSeconds { get; init; } = 60;
}
