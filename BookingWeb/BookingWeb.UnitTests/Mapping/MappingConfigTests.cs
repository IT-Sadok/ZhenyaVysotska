using BookingWeb.Application.Common.Mapping;
using BookingWeb.Application.Listings;
using BookingWeb.Application.Profiles;
using BookingWeb.Domain.Models;
using Mapster;
using Shouldly;

namespace BookingWeb.UnitTests.Mapping;

public sealed class MappingConfigTests
{
    [Fact]
    public void RegisterMappings_ShouldProduceValidConfig_WhenCompiled()
    {
        var config = new TypeAdapterConfig();
        MappingConfig.RegisterMappings(config);

        Should.NotThrow(() => config.Compile());
    }

    [Fact]
    public void Adapt_ShouldMapAllFields_WhenMappingListingToDto()
    {
        var hostId = Guid.NewGuid();
        var listing = Listing.Create(
            hostId, "Loft", "Cozy", "Kyiv", "Khreshchatyk 1", 1500m, 2, DateTimeOffset.UtcNow);

        var dto = listing.Adapt<ListingDto>();

        dto.Id.ShouldBe(listing.Id);
        dto.HostId.ShouldBe(hostId);
        dto.Title.ShouldBe("Loft");
        dto.Description.ShouldBe("Cozy");
        dto.City.ShouldBe("Kyiv");
        dto.Address.ShouldBe("Khreshchatyk 1");
        dto.PricePerNight.ShouldBe(1500m);
        dto.MaxGuests.ShouldBe(2);
        dto.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Adapt_ShouldMapNullDescription_WhenListingHasNoDescription()
    {
        var listing = Listing.Create(
            Guid.NewGuid(), "Loft", null, "Kyiv", "Khreshchatyk 1", 1500m, 2, DateTimeOffset.UtcNow);

        var dto = listing.Adapt<ListingDto>();

        dto.Description.ShouldBeNull();
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
}
