using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BookingWeb.Application.Common.Constants;
using BookingWeb.Application.Geocoding;
using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Results;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookingWeb.Infrastructure.Geocoding;

public sealed class NominatimGeocodingService : IGeocodingService
{
    private static readonly string[] CityKeys = ["city", "town", "village", "municipality"];

    private const int ReverseCoordinateDecimals = 5;

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly IOptionsMonitor<NominatimSettings> _settings;
    private readonly ILogger<NominatimGeocodingService> _logger;

    public NominatimGeocodingService(
        HttpClient http,
        IMemoryCache cache,
        IOptionsMonitor<NominatimSettings> settings,
        ILogger<NominatimGeocodingService> logger)
    {
        _http = http;
        _cache = cache;
        _settings = settings;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<GeocodingCandidateDto>>> SearchAsync(
        string query, CancellationToken ct = default)
    {
        var settings = _settings.CurrentValue;
        if (!settings.Enabled)
        {
            return GeocodingErrors.Disabled;
        }

        var normalized = NormalizeQuery(query);
        var cacheKey = $"geocoding:search:{settings.AcceptLanguage}:{normalized.ToLowerInvariant()}";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<GeocodingCandidateDto>? cached))
        {
            return Result.Success(cached!);
        }

        var url = $"search?format=jsonv2&limit={settings.MaxSearchResults}" +
                  $"&accept-language={Uri.EscapeDataString(settings.AcceptLanguage)}" +
                  $"&q={Uri.EscapeDataString(normalized)}";

        var response = await GetAsync<NominatimPlace[]>(url, ct);
        if (response.IsFailure)
        {
            return response.Error;
        }

        IReadOnlyList<GeocodingCandidateDto> candidates = response.Value.Select(ToCandidate).ToList();

        Store(cacheKey, candidates, candidates.Count == 0 
            ? settings.EmptyResultCacheDuration 
            : settings.CacheDuration);
        return Result.Success(candidates);
    }

    public async Task<Result<ReverseGeocodingDto>> ReverseAsync(
        double latitude, double longitude, CancellationToken ct = default)
    {
        var settings = _settings.CurrentValue;
        if (!settings.Enabled)
        {
            return GeocodingErrors.Disabled;
        }
        
        var lat = FormatCoordinate(latitude);
        var lng = FormatCoordinate(longitude);
        var cacheKey = $"geocoding:reverse:{settings.AcceptLanguage}:{lat}:{lng}";

        if (_cache.TryGetValue(cacheKey, out ReverseGeocodingDto? cached))
        {
            return cached!;
        }

        var url = $"reverse?format=jsonv2&addressdetails=1" +
                  $"&accept-language={Uri.EscapeDataString(settings.AcceptLanguage)}" +
                  $"&lat={lat}&lon={lng}";

        var response = await GetAsync<NominatimReverseResponse>(url, ct);
        if (response.IsFailure)
        {
            return response.Error;
        }

        if (response.Value.Error is not null || response.Value.DisplayName is null)
        {
            return GeocodingErrors.NotFound;
        }

        var address = ToAddress(response.Value);
        Store(cacheKey, address, settings.CacheDuration);
        return address;
    }

    private async Task<Result<T>> GetAsync<T>(string url, CancellationToken ct)
    {
        try
        {
            using var response = await _http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Nominatim returned {StatusCode} for {Url}", (int)response.StatusCode, url);
                return GeocodingErrors.Unavailable;
            }

            var body = await response.Content.ReadFromJsonAsync<T>(ct);
            if (body is null)
            {
                return GeocodingErrors.Unavailable;
            }

            return body;
        }
        catch (NominatimRateLimitExceededException)
        {
            _logger.LogWarning("Nominatim request rejected: rate limit queue is full");
            return GeocodingErrors.Busy;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Nominatim request timed out for {Url}", url);
            return GeocodingErrors.Unavailable;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            _logger.LogWarning(exception, "Nominatim request failed for {Url}", url);
            return GeocodingErrors.Unavailable;
        }
    }

    private void Store<T>(string key, T value, TimeSpan duration)
    {
        _cache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = duration,
            Size = 1
        });
    }
    
    internal static string NormalizeQuery(string query)
    {
        var words = query.Normalize(NormalizationForm.FormC)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return string.Join(' ', words);
    }

    private static GeocodingCandidateDto ToCandidate(NominatimPlace place)
    {
        var box = place.BoundingBox.Select(ParseDouble).ToArray();

        return new GeocodingCandidateDto(
            place.DisplayName,
            ParseDouble(place.Lat),
            ParseDouble(place.Lon),
            new GeocodingBoundingBoxDto(MinLat: box[0], MinLng: box[2], MaxLat: box[1], MaxLng: box[3]));
    }

    private static ReverseGeocodingDto ToAddress(NominatimReverseResponse response)
    {
        var address = response.Address ?? [];

        var city = CityKeys
            .Select(key => address.GetValueOrDefault(key))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        var countryCode = address.GetValueOrDefault("country_code")?.ToUpperInvariant();

        return new ReverseGeocodingDto(
            response.DisplayName!,
            city,
            address.GetValueOrDefault("state"),
            countryCode is not null && CountryCodes.IsValid(countryCode) ? countryCode : null);
    }

    private static double ParseDouble(string value) => double.Parse(value, CultureInfo.InvariantCulture);

    internal static string FormatCoordinate(double value) =>
        Math.Round(value, ReverseCoordinateDecimals, MidpointRounding.AwayFromZero)
            .ToString("R", CultureInfo.InvariantCulture);
}
