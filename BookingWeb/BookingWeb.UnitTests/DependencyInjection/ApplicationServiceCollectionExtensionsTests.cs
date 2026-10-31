using BookingWeb.Application;
using BookingWeb.Application.Admin;
using BookingWeb.Application.Auth;
using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Listings;
using BookingWeb.Application.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BookingWeb.UnitTests.DependencyInjection;

public sealed class ApplicationServiceCollectionExtensionsTests
{
    public static TheoryData<Type, Type> ExpectedServices => new()
    {
        { typeof(IAuthService), typeof(AuthService) },
        { typeof(IAdminService), typeof(AdminService) },
        { typeof(IProfileService), typeof(ProfileService) },
        { typeof(IRefreshTokenService), typeof(RefreshTokenService) },
        { typeof(IListingService), typeof(ListingService) },
    };

    [Theory]
    [MemberData(nameof(ExpectedServices))]
    public void AddApplication_ShouldRegisterServiceAsScoped_WhenServiceFollowsNamingConvention(
        Type serviceType, Type implementationType)
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var descriptor = services.Where(d => d.ServiceType == serviceType).ShouldHaveSingleItem();
        descriptor.ImplementationType.ShouldBe(implementationType);
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddApplication_ShouldNotRegisterServiceTwice_WhenCalledOnce()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        services
            .Where(d => d.ServiceType.Name.EndsWith("Service"))
            .GroupBy(d => d.ServiceType)
            .ShouldAllBe(group => group.Count() == 1);
    }
}
