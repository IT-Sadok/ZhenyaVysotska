namespace BookingWeb.Infrastructure.Geocoding;

public sealed class NominatimSettings
{
    public const string SectionName = "Nominatim";

    public bool Enabled { get; init; } = true;

    public string BaseUrl { get; init; } = "https://nominatim.openstreetmap.org/";
    
    public string UserAgent { get; init; } = string.Empty;

    public string AcceptLanguage { get; init; } = "uk,en";

    public int TimeoutSeconds { get; init; } = 5;

    public int QueueLimit { get; init; } = 3;

    public int MaxSearchResults { get; init; } = 5;

    public TimeSpan CacheDuration { get; init; } = TimeSpan.FromDays(30);

    public TimeSpan EmptyResultCacheDuration { get; init; } = TimeSpan.FromDays(1);
}
