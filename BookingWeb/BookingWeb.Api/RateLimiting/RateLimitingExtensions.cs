using System.Globalization;
using System.Threading.RateLimiting;
using BookingWeb.Api.Extensions;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace BookingWeb.Api.RateLimiting;

public static class RateLimitingExtensions
{
    public const string GeocodingSearchPolicy = "geocoding-search";

    public const string RejectedErrorCode = "RateLimit.Exceeded";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GeocodingSearchRateLimitSettings>()
            .Bind(configuration.GetSection(GeocodingSearchRateLimitSettings.SectionName))
            .Validate(s => s is { PermitLimit: > 0, WindowSeconds: > 0 },
                $"'{GeocodingSearchRateLimitSettings.SectionName}' permit limit and window must be positive.")
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionAsync;
            
            options.AddPolicy(GeocodingSearchPolicy, context =>
            {
                var settings = context.RequestServices
                    .GetRequiredService<IOptions<GeocodingSearchRateLimitSettings>>().Value;

                return RateLimitPartition.GetFixedWindowLimiter(GetPartitionKey(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.PermitLimit,
                    Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });
        });

        return services;
    }
    
    internal static string GetPartitionKey(HttpContext context)
    {
        var userId = context.User.GetUserId();
        if (userId is not null)
        {
            return $"user:{userId}";
        }

        return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }
    
    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken ct)
    {
        var http = context.HttpContext;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            http.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        var problemDetails = http.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails =
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = RejectedErrorCode,
                Detail = "Too many requests. Try again later."
            }
        });
    }
}
