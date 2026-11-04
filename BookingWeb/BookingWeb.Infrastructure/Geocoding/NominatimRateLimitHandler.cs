using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace BookingWeb.Infrastructure.Geocoding;

public sealed class NominatimRateLimitHandler : DelegatingHandler
{
    public const string LimiterKey = "nominatim";

    private readonly RateLimiter _limiter;
    
    public NominatimRateLimitHandler([FromKeyedServices(LimiterKey)] RateLimiter limiter)
    {
        _limiter = limiter;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var lease = await _limiter.AcquireAsync(permitCount: 1, cancellationToken);

        if (!lease.IsAcquired)
        {
            throw new NominatimRateLimitExceededException();
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

public sealed class NominatimRateLimitExceededException()
    : Exception("Nominatim rate limit queue is full.");
