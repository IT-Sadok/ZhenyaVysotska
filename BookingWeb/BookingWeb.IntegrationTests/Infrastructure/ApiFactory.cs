using System.Threading.RateLimiting;
using BookingWeb.Application.Interfaces;
using BookingWeb.Infrastructure.Geocoding;
using BookingWeb.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace BookingWeb.IntegrationTests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@integration.test";
    public const string AdminPassword = "Admin123!";
    
    private const string JwtSecret = "integration-tests-jwt-secret-0123456789abcdef";

    private static readonly string[] EnvironmentVariableNames =
    [
        "ASPNETCORE_ENVIRONMENT",
        "ConnectionStrings__Default",
        "Jwt__Secret",
        "Seed__AdminEmail",
        "Seed__AdminPassword"
    ];
    
    private readonly TokenBucketRateLimiter _nominatimLimiter = new(new TokenBucketRateLimiterOptions
    {
        TokenLimit = 1,
        TokensPerPeriod = 1,
        ReplenishmentPeriod = TimeSpan.FromTicks(1),
        QueueLimit = 0,
        AutoReplenishment = false
    });

    public FakeNominatimHandler Nominatim { get; } = new();

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgis/postgis:16-3.6").Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var connectionString = _postgres.GetConnectionString();
        
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", connectionString);
        Environment.SetEnvironmentVariable("Jwt__Secret", JwtSecret);
        Environment.SetEnvironmentVariable("Seed__AdminEmail", AdminEmail);
        Environment.SetEnvironmentVariable("Seed__AdminPassword", AdminPassword);
        
        await MigrateAsync(connectionString);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateOnBuild = true;
            options.ValidateScopes = true;
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient(nameof(IGeocodingService))
                .ConfigurePrimaryHttpMessageHandler(() => Nominatim);

            services.AddKeyedSingleton<RateLimiter>(NominatimRateLimitHandler.LimiterKey, _nominatimLimiter);

            services.AddTransient<IStartupFilter, TestClientIpStartupFilter>();
        });
    }
    
    public void ReplenishNominatimLimit() => _nominatimLimiter.TryReplenish();

    public void ResetGeocoding()
    {
        Nominatim.Reset();
        ReplenishNominatimLimit();
        ((MemoryCache)Services.GetRequiredKeyedService<IMemoryCache>(NominatimGeocodingService.CacheKey)).Clear();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await db.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                "Listings",
                "RefreshTokens",
                "UserProfiles",
                "AspNetUserRoles",
                "AspNetUserClaims",
                "AspNetUserLogins",
                "AspNetUserTokens",
                "AspNetUsers"
            RESTART IDENTITY CASCADE;
            """);

        await DbSeeder.SeedAsync(scope.ServiceProvider);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();

        foreach (var name in EnvironmentVariableNames)
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    private static async Task MigrateAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite())
            .Options;

        await using var db = new ApplicationDbContext(options);
        await db.Database.MigrateAsync();
    }
}
