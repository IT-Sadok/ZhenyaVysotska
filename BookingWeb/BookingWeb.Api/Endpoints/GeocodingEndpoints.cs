using BookingWeb.Api.Extensions;
using BookingWeb.Api.RateLimiting;
using BookingWeb.Application.Common.Constants;
using BookingWeb.Application.Geocoding.Requests;
using BookingWeb.Application.Interfaces;

namespace BookingWeb.Api.Endpoints;

public sealed class GeocodingEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/geocoding")
            .WithTags("Geocoding");
        
        group.MapGet("/search", async (
                [AsParameters] GeocodingSearchRequest request, IGeocodingService geocoding, CancellationToken ct) =>
            {
                var result = await geocoding.SearchAsync(request.Q!, ct);
                return result.Match(Results.Ok);
            })
            .RequireRateLimiting(RateLimitingExtensions.GeocodingSearchPolicy)
            .WithDescription("Finds places for a \"street, city\" query.");

        group.MapGet("/reverse", async (
                [AsParameters] ReverseGeocodingRequest request, IGeocodingService geocoding, CancellationToken ct) =>
            {
                var result = await geocoding.ReverseAsync(request.Lat!.Value, request.Lng!.Value, ct);
                return result.Match(Results.Ok);
            })
            .RequireAuthorization(PolicyNames.HostOnly)
            .WithDescription("Finds city and region for coordinates.");
    }
}
