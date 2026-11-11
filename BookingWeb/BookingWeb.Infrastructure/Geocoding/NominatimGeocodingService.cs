using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
        var normalized = NormalizeQuery(query);
        var cacheKey = $"geocoding:search:{settings.AcceptLanguage}:{settings.CountryCodes}:{normalized.ToLowerInvariant()}";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<GeocodingCandidateDto>? cached))
        {
            return Result.Success(cached!);
        }

        var url = $"search?format=jsonv2&limit={settings.MaxSearchResults}" +
                  $"&accept-language={Uri.EscapeDataString(settings.AcceptLanguage)}" +
                  $"&q={Uri.EscapeDataString(normalized)}";

        if (!string.IsNullOrWhiteSpace(settings.CountryCodes))
        {
            url += $"&countrycodes={Uri.EscapeDataString(settings.CountryCodes)}";
        }

        var response = await GetAsync<NominatimPlace[]>("search", url, ct);
        if (response.IsFailure)
        {
            return response.Error;
        }

        var candidates = new List<GeocodingCandidateDto>(response.Value.Length);
        foreach (var place in response.Value)
        {
            if (!TryToCandidate(place, out var candidate))
            {
                _logger.LogWarning("Nominatim search returned a malformed place");
                return GeocodingErrors.Unavailable;
            }

            candidates.Add(candidate);
        }

        Store(cacheKey, candidates, candidates.Count == 0 
            ? settings.EmptyResultCacheDuration 
            : settings.CacheDuration);
        return Result.Success<IReadOnlyList<GeocodingCandidateDto>>(candidates);
    }

    public async Task<Result<ReverseGeocodingDto>> ReverseAsync(
        double latitude, double longitude, CancellationToken ct = default)
    {
        var settings = _settings.CurrentValue;
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

        var response = await GetAsync<NominatimReverseResponse>("reverse", url, ct);
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

    // url contains the user's search query / coordinates (PII) - never log it, log only operation.
    private async Task<Result<T>> GetAsync<T>(string operation, string url, CancellationToken ct)
    {
        try
        {
            using var response = await _http.GetAsync(url, ct);
           
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (NominatimRateLimitHandler.IsRejectedLocally(response))
                {
                    _logger.LogWarning("Nominatim {Operation} request rejected locally: rate limit queue is full",
                        operation);
                }
                else
                {
                    _logger.LogError("Nominatim {Operation} returned 429: Nominatim is throttling us", operation);
                }

                return GeocodingErrors.Busy;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Nominatim {Operation} returned {StatusCode}", operation, (int)response.StatusCode);
                return GeocodingErrors.Unavailable;
            }

            var body = await response.Content.ReadFromJsonAsync<T>(ct);
            if (body is null)
            {
                return GeocodingErrors.Unavailable;
            }

            return body;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Nominatim {Operation} request timed out", operation);
            return GeocodingErrors.Unavailable;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            _logger.LogWarning(exception, "Nominatim {Operation} request failed", operation);
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
    
    private static bool TryToCandidate(NominatimPlace place, [NotNullWhen(true)] out GeocodingCandidateDto? candidate)
    {
        candidate = null;

        if (string.IsNullOrWhiteSpace(place.DisplayName)
            || !TryParseDouble(place.Lat, out var lat)
            || !TryParseDouble(place.Lon, out var lng)
            || place.BoundingBox is not { Length: 4 } box
            || !TryParseDouble(box[0], out var minLat)
            || !TryParseDouble(box[1], out var maxLat)
            || !TryParseDouble(box[2], out var minLng)
            || !TryParseDouble(box[3], out var maxLng))
        {
            return false;
        }

        candidate = new GeocodingCandidateDto(
            place.DisplayName, lat, lng, new GeocodingBoundingBoxDto(minLat, minLng, maxLat, maxLng));
        return true;
    }

    private static ReverseGeocodingDto ToAddress(NominatimReverseResponse response)
    {
        var address = response.Address ?? [];

        var city = CityKeys
            .Select(key => address.GetValueOrDefault(key))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        return new ReverseGeocodingDto(
            response.DisplayName!,
            city,
            address.GetValueOrDefault("state"));
    }

    private static bool TryParseDouble(string? value, out double result) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) && double.IsFinite(result);

    internal static string FormatCoordinate(double value) =>
        Math.Round(value, ReverseCoordinateDecimals, MidpointRounding.AwayFromZero)
            .ToString("R", CultureInfo.InvariantCulture);
}
