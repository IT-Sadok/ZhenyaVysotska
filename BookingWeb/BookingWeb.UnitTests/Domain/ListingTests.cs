using BookingWeb.Domain.Models;
using Shouldly;

namespace BookingWeb.UnitTests.Domain;

public sealed class ListingTests
{
    private static readonly Guid HostId = Guid.NewGuid();
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private static Listing CreateListing()
    {
        return Listing.Create(HostId, "Loft", "Cozy", "Kyiv", "Khreshchatyk 1", 1500m, 2, CreatedAt);
    }

    [Fact]
    public void Create_ShouldCreateActiveListing_WhenArgumentsAreValid()
    {
        var listing = Listing.Create(
            HostId, "  Loft  ", "  Cozy  ", " Kyiv ", " Khreshchatyk 1 ", 1500m, 2, CreatedAt);

        listing.Id.ShouldNotBe(Guid.Empty);
        listing.HostId.ShouldBe(HostId);
        listing.IsActive.ShouldBeTrue();
        listing.Title.ShouldBe("Loft");
        listing.Description.ShouldBe("Cozy");
        listing.City.ShouldBe("Kyiv");
        listing.Address.ShouldBe("Khreshchatyk 1");
        listing.CreatedAtUtc.ShouldBe(CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_ShouldStoreNullDescription_WhenDescriptionIsBlank(string? description)
    {
        var listing = Listing.Create(HostId, "Loft", description, "Kyiv", "Khreshchatyk 1", 1500m, 2, CreatedAt);

        listing.Description.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldThrow_WhenHostIdIsEmpty()
    {
        Should.Throw<ArgumentException>(() =>
            Listing.Create(Guid.Empty, "Loft", null, "Kyiv", "Khreshchatyk 1", 1500m, 2, CreatedAt));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldThrow_WhenPriceIsNotPositive(int price)
    {
        Should.Throw<ArgumentException>(() =>
            Listing.Create(HostId, "Loft", null, "Kyiv", "Khreshchatyk 1", price, 2, CreatedAt));
    }

    [Fact]
    public void Create_ShouldThrow_WhenMaxGuestsIsZero()
    {
        Should.Throw<ArgumentException>(() =>
            Listing.Create(HostId, "Loft", null, "Kyiv", "Khreshchatyk 1", 1500m, 0, CreatedAt));
    }

    [Fact]
    public void Update_ShouldKeepOldValues_WhenNewValuesAreInvalid()
    {
        var listing = CreateListing();

        Should.Throw<ArgumentException>(() =>
            listing.Update("New title", null, "Lviv", "Rynok 1", pricePerNight: 0m, maxGuests: 4));

        listing.Title.ShouldBe("Loft");
        listing.City.ShouldBe("Kyiv");
        listing.PricePerNight.ShouldBe(1500m);
    }

    [Fact]
    public void Deactivate_ShouldMakeListingInactive_WhenListingIsActive()
    {
        var listing = CreateListing();

        listing.Deactivate();

        listing.IsActive.ShouldBeFalse();
    }
}
