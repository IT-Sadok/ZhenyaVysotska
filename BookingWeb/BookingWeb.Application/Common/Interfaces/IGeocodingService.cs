using BookingWeb.Application.Geocoding;
using BookingWeb.Application.Results;

namespace BookingWeb.Application.Common.Interfaces;

public interface IGeocodingService
{
    Task<Result<IReadOnlyList<GeocodingCandidateDto>>> SearchAsync(string query, CancellationToken ct = default);

    Task<Result<ReverseGeocodingDto>> ReverseAsync(double latitude, double longitude, CancellationToken ct = default);
}
