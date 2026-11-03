using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using BookingWeb.Application.Common.Settings;
using BookingWeb.Application.Common.Constants;
using BookingWeb.Application.Interfaces;
using BookingWeb.Domain;
using BookingWeb.Infrastructure.Authentication;
using BookingWeb.Infrastructure.Geocoding;
using BookingWeb.Infrastructure.Identity;
using BookingWeb.Infrastructure.Persistence;
using BookingWeb.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BookingWeb.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    private const int MinJwtSecretLengthInBytes = 32;
 
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddIdentityServices();
        services.AddTokenServices(configuration);
        services.AddAuthenticationAndAuthorization(configuration);
        services.AddGeocoding(configuration);
 
        return services;
    }
 
    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' is not configured. ");
 
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite()));
        
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        services.Scan(scan => scan
            .FromAssemblyOf<ListingRepository>()
            .AddClasses(classes => classes
                .InNamespaceOf<ListingRepository>()
                .Where(type => type.Name.EndsWith("Repository")))
            .AsMatchingInterface()
            .WithScopedLifetime());
    }
 
    private static void AddIdentityServices(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
 
        services.AddScoped<IIdentityService, IdentityService>();
    }
 
    private static void AddTokenServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        
        services.Configure<RefreshTokenSettings>(configuration.GetSection(RefreshTokenSettings.SectionName));
 
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
    }
 
    private static void AddAuthenticationAndAuthorization(
        this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = GetValidatedJwtSettings(configuration);
 
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
 
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    RoleClaimType = "role",
                    NameClaimType = "sub"
                };
            });
 
        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.AdminOnly, policy => policy.RequireRole(Roles.Admin))
            .AddPolicy(PolicyNames.HostOnly, policy => policy.RequireRole(Roles.Host));
    }

    private static void AddGeocoding(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<NominatimSettings>()
            .Bind(configuration.GetSection(NominatimSettings.SectionName))
            .Validate(s => Uri.TryCreate(s.BaseUrl, UriKind.Absolute, out _), "'Nominatim:BaseUrl' must be an absolute URL.")
            .Validate(s => !string.IsNullOrWhiteSpace(s.UserAgent),
                "'Nominatim:UserAgent' must identify the application (Nominatim usage policy).")
            .Validate(s => s is { TimeoutSeconds: > 0, QueueLimit: >= 0, MaxSearchResults: > 0 },
                "'Nominatim' timeout, queue limit and max results must be positive.")
            .ValidateOnStart();
        
        services.AddMemoryCache(options => options.SizeLimit = 10_000);

        services.AddKeyedSingleton<RateLimiter>(NominatimRateLimitHandler.LimiterKey, (provider, _) =>
        {
            var settings = provider.GetRequiredService<IOptions<NominatimSettings>>().Value;

            return new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                TokensPerPeriod = 1,
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                QueueLimit = settings.QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
        });

        services.AddTransient<NominatimRateLimitHandler>();

        services.AddHttpClient<IGeocodingService, NominatimGeocodingService>((provider, client) =>
            {
                var settings = provider.GetRequiredService<IOptions<NominatimSettings>>().Value;

                client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", settings.UserAgent);
            })
            .AddHttpMessageHandler<NominatimRateLimitHandler>();
    }

    private static JwtSettings GetValidatedJwtSettings(IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{JwtSettings.SectionName}' is missing.");
 
        if (string.IsNullOrWhiteSpace(jwtSettings.Secret))
        {
            throw new InvalidOperationException(
                $"'{JwtSettings.SectionName}:Secret' is not configured. Set it in user-secrets.");
        }
 
        if (Encoding.UTF8.GetByteCount(jwtSettings.Secret) < MinJwtSecretLengthInBytes)
        {
            throw new InvalidOperationException(
                $"'{JwtSettings.SectionName}:Secret' must be at least {MinJwtSecretLengthInBytes} bytes for HS256.");
        }
 
        return jwtSettings;
    }
}
