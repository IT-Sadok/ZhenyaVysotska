using System.Net;
using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace BookingWeb.Infrastructure.Geocoding;

public sealed class NominatimRateLimitHandler : DelegatingHandler
{
    public const string LimiterKey = "nominatim";
    
    private static readonly HttpRequestOptionsKey<bool> RejectedLocallyKey = new("Nominatim.RejectedLocally");

    private readonly RateLimiter _limiter;

    public NominatimRateLimitHandler([FromKeyedServices(LimiterKey)] RateLimiter limiter)
    {
        _limiter = limiter;
    }
    
    public static bool IsRejectedLocally(HttpResponseMessage response) =>
        response.RequestMessage?.Options.TryGetValue(RejectedLocallyKey, out var rejected) == true && rejected;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var lease = await _limiter.AcquireAsync(permitCount: 1, cancellationToken);

        if (!lease.IsAcquired)
        {
            request.Options.Set(RejectedLocallyKey, true);
            return new HttpResponseMessage(HttpStatusCode.TooManyRequests) { RequestMessage = request };
        }
        
        return await base.SendAsync(request, cancellationToken);
    }
}
