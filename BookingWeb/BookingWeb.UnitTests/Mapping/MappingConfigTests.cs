using BookingWeb.Application.Bookings;
using BookingWeb.Application.Common.Mapping;
using BookingWeb.Application.Listings;
using BookingWeb.Application.Profiles;
using BookingWeb.Domain.Entities;
using BookingWeb.Domain.ValueObjects;
using Mapster;
using Shouldly;

namespace BookingWeb.UnitTests.Mapping;

public sealed class MappingConfigTests
{
    private static readonly GeoPoint Exact = new(50.4501, 30.5234);
    private static readonly GeoPoint Public = new(50.4530, 30.5260);

    private static Listing CreateListing(Guid? hostId = null, string? description = "Cozy", string? region = "Kyiv") =>
        Listing.Create(
            hostId ?? Guid.NewGuid(), "Loft", description, region, "Kyiv", "Khreshchatyk 1",
            location: Exact, publicLocation: Public, 1500m, 2, DateTimeOffset.UtcNow);

    [Fact]
    public void RegisterMappings_ShouldProduceValidConfig_WhenCompiled()
    {
        var config = new TypeAdapterConfig();
        MappingConfig.RegisterMappings(config);

        Should.NotThrow(() => config.Compile());
    }

    [Fact]
    public void RegisterMappings_ShouldProduceValidProjections_WhenCompiledForQueryables()
    {
        // ProjectToType (used by the repository) builds a separate expression tree from Adapt,
        // so it has to be validated on its own.
        var config = new TypeAdapterConfig();
        MappingConfig.RegisterMappings(config);

        Should.NotThrow(() => config.CompileProjection());
    }

    [Fact]
    public void RegisterMappings_ShouldEnableRequireDestinationMemberSource_WhenCalled()
    {
        // Without this flag Mapster silently leaves unmapped DTO members at default values,
        // so a new DTO property (e.g. Address on a public DTO) could slip through unnoticed.
        var config = new TypeAdapterConfig();

        MappingConfig.RegisterMappings(config);

        config.RequireDestinationMemberSource.ShouldBeTrue();
    }

    [Theory]
    [InlineData(typeof(PublicListingDto))]
    [InlineData(typeof(HostListingDto))]
    [InlineData(typeof(ListingMarkerDto))]
    public void Mapster_ShouldFailCompile_WhenRequireSourceIsOnAndListingDtoMemberIsUnmapped(Type dtoType)
    {
        // Documents the Mapster behaviour we rely on (not our config): with RequireDestinationMemberSource on,
        // a DTO member without a source makes Compile() fail. Our config enabling the flag is checked above.
        var config = new TypeAdapterConfig { RequireDestinationMemberSource = true };
        config.NewConfig(typeof(Listing), dtoType);

        Should.Throw<Exception>(() => config.Compile());
    }

    [Fact]
    public void PublicDtos_ShouldNotHaveAddressMember_WhenDefined()
    {
        typeof(PublicListingDto).GetProperty("Address").ShouldBeNull();
        typeof(ListingMarkerDto).GetProperty("Address").ShouldBeNull();
    }

    [Fact]
    public void Adapt_ShouldUsePublicPoint_WhenMappingListingToPublicDto()
    {
        var hostId = Guid.NewGuid();
        var listing = CreateListing(hostId);

        var dto = listing.Adapt<PublicListingDto>();

        dto.Id.ShouldBe(listing.Id);
        dto.HostId.ShouldBe(hostId);
        dto.Title.ShouldBe("Loft");
        dto.Description.ShouldBe("Cozy");
        dto.Region.ShouldBe("Kyiv");
        dto.City.ShouldBe("Kyiv");
        dto.Latitude.ShouldBe(Public.Latitude);
        dto.Longitude.ShouldBe(Public.Longitude);
        dto.PricePerNight.ShouldBe(1500m);
        dto.MaxGuests.ShouldBe(2);
    }

    [Fact]
    public void Adapt_ShouldUseExactPointAndAddress_WhenMappingListingToHostDto()
    {
        var listing = CreateListing();

        var dto = listing.Adapt<HostListingDto>();

        dto.Address.ShouldBe("Khreshchatyk 1");
        dto.Latitude.ShouldBe(Exact.Latitude);
        dto.Longitude.ShouldBe(Exact.Longitude);
        dto.PublicLatitude.ShouldBe(Public.Latitude);
        dto.PublicLongitude.ShouldBe(Public.Longitude);
        dto.IsActive.ShouldBeTrue();
        dto.CreatedAtUtc.ShouldBe(listing.CreatedAtUtc);
    }

    [Fact]
    public void Adapt_ShouldUsePublicPoint_WhenMappingListingToMarker()
    {
        var listing = CreateListing();

        var marker = listing.Adapt<ListingMarkerDto>();

        marker.Id.ShouldBe(listing.Id);
        marker.Title.ShouldBe("Loft");
        marker.PricePerNight.ShouldBe(1500m);
        marker.Latitude.ShouldBe(Public.Latitude);
        marker.Longitude.ShouldBe(Public.Longitude);
    }

    [Fact]
    public void Adapt_ShouldMapNullOptionalFields_WhenListingHasNoDescriptionOrRegion()
    {
        var listing = CreateListing(description: null, region: null);

        var dto = listing.Adapt<PublicListingDto>();

        dto.Description.ShouldBeNull();
        dto.Region.ShouldBeNull();
    }

    [Fact]
    public void Adapt_ShouldMapAllFields_WhenMappingUserProfileToDto()
    {
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, "Ivan", "Petrenko");
        profile.UpdateBio("Traveler");
        profile.UpdateAvatar("https://example.com/a.png");

        var dto = profile.Adapt<ProfileDto>();

        dto.UserId.ShouldBe(userId);
        dto.FirstName.ShouldBe("Ivan");
        dto.LastName.ShouldBe("Petrenko");
        dto.Bio.ShouldBe("Traveler");
        dto.AvatarUrl.ShouldBe("https://example.com/a.png");
    }

    [Fact]
    public void Adapt_ShouldMapNullOptionalFields_WhenProfileHasNoBioOrAvatar()
    {
        var profile = UserProfile.Create(Guid.NewGuid(), "Ivan", "Petrenko");

        var dto = profile.Adapt<ProfileDto>();

        dto.Bio.ShouldBeNull();
        dto.AvatarUrl.ShouldBeNull();
    }

    [Fact]
    public void Adapt_ShouldMapAllFields_WhenMappingConfirmedBookingToDto()
    {
        var createdAt = new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
        var listingId = Guid.NewGuid();
        var booking = Booking.Create(
            listingId, Guid.NewGuid(), new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 13), 2, 1000m, createdAt);

        var dto = booking.Adapt<BookingDto>();

        dto.Id.ShouldBe(booking.Id);
        dto.ListingId.ShouldBe(listingId);
        dto.CheckIn.ShouldBe(new DateOnly(2026, 3, 10));
        dto.CheckOut.ShouldBe(new DateOnly(2026, 3, 13));
        dto.Nights.ShouldBe(3);
        dto.Guests.ShouldBe(2);
        dto.TotalPrice.ShouldBe(3000m);
        dto.Status.ShouldBe("Confirmed");
        dto.CreatedAtUtc.ShouldBe(createdAt);
        dto.CancelledAtUtc.ShouldBeNull();
        dto.CancelledBy.ShouldBeNull();
    }

    [Fact]
    public void Adapt_ShouldMapCancellationAsStrings_WhenBookingIsCancelledByHost()
    {
        var booking = Booking.Create(
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 13), 2, 1000m,
            new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero));
        var cancelledAt = new DateTimeOffset(2026, 3, 2, 9, 0, 0, TimeSpan.Zero);
        booking.Cancel(cancelledAt, DateOnly.FromDateTime(cancelledAt.UtcDateTime), BookingCanceller.Host);

        var dto = booking.Adapt<BookingDto>();

        dto.Status.ShouldBe("Cancelled");
        dto.CancelledAtUtc.ShouldBe(cancelledAt);
        dto.CancelledBy.ShouldBe("Host");
    }
}
