using BookingWeb.Application.Common.Interfaces;
using BookingWeb.Infrastructure.Authentication;
using BookingWeb.Infrastructure.Geocoding;
using BookingWeb.Infrastructure.Identity;
using BookingWeb.Infrastructure.Persistence;
using BookingWeb.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BookingWeb.IntegrationTests.Infrastructure;

[Collection(ApiCollection.Name)]
public sealed class DependencyInjectionTests
{
    private readonly ApiFactory _factory;

    public DependencyInjectionTests(ApiFactory factory)
    {
        _factory = factory;
    }

    public static TheoryData<Type, Type> ExpectedInfrastructureServices => new()
    {
        { typeof(IUserRepository), typeof(UserRepository) },
        { typeof(IUserProfileRepository), typeof(UserProfileRepository) },
        { typeof(IRefreshTokenRepository), typeof(RefreshTokenRepository) },
        { typeof(IListingRepository), typeof(ListingRepository) },
        { typeof(IUnitOfWork), typeof(UnitOfWork) },
        { typeof(IIdentityService), typeof(IdentityService) },
        { typeof(IJwtTokenGenerator), typeof(JwtTokenGenerator) },
        { typeof(IGeocodingService), typeof(NominatimGeocodingService) },
    };

    [Theory]
    [MemberData(nameof(ExpectedInfrastructureServices))]
    public void Services_ShouldResolveExpectedImplementation_WhenRequested(
        Type serviceType, Type implementationType)
    {
        using var scope = _factory.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService(serviceType);

        service.ShouldBeOfType(implementationType);
    }

    [Fact]
    public void JwtTokenGenerator_ShouldBeSingleton_WhenResolvedFromDifferentScopes()
    {
        using var first = _factory.Services.CreateScope();
        using var second = _factory.Services.CreateScope();

        var a = first.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var b = second.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

        a.ShouldBeSameAs(b);
    }

    [Fact]
    public void SharedMemoryCache_ShouldAcceptEntriesWithoutSize_WhenSomeoneAddsItNextToGeocodingCache()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddMemoryCache()));

        var shared = factory.Services.GetRequiredService<IMemoryCache>();
        var geocoding = factory.Services.GetRequiredKeyedService<IMemoryCache>(NominatimGeocodingService.CacheKey);

        Should.NotThrow(() => shared.Set("some-feature:key", "value"));
        shared.ShouldNotBeSameAs(geocoding);
    }

    [Fact]
    public void Repositories_ShouldBeScoped_WhenResolvedFromDifferentScopes()
    {
        using var first = _factory.Services.CreateScope();
        using var second = _factory.Services.CreateScope();

        var sameScopeA = first.ServiceProvider.GetRequiredService<IListingRepository>();
        var sameScopeB = first.ServiceProvider.GetRequiredService<IListingRepository>();
        var otherScope = second.ServiceProvider.GetRequiredService<IListingRepository>();

        sameScopeA.ShouldBeSameAs(sameScopeB);
        sameScopeA.ShouldNotBeSameAs(otherScope);
    }
}
