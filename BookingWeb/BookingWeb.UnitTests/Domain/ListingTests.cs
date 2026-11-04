using BookingWeb.Domain.Entities;
using BookingWeb.Domain.ValueObjects;
using Shouldly;

namespace BookingWeb.UnitTests.Domain;

public sealed class ListingTests
{
    private static readonly Guid HostId = Guid.NewGuid();
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly GeoPoint Exact = new(48.2922, 24.5546);
    private static readonly GeoPoint Public = new(48.2950, 24.5580);
    private const string Kyiv = "Europe/Kyiv";

    private static Listing CreateListing(
        Guid? hostId = null,
        string title = "Loft",
        string? description = "Cozy",
        string countryCode = "UA",
        string? region = "Ivano-Frankivsk Oblast",
        string city = "Vorokhta",
        string address = "Danyla Halytskoho 1",
        GeoPoint? location = null,
        GeoPoint? publicLocation = null,
        string timeZoneId = Kyiv,
        decimal pricePerNight = 1500m,
        int maxGuests = 2)
    {
        return Listing.Create(
            hostId ?? HostId,
            title,
            description,
            countryCode,
            region,
            city,
            address,
            location ?? Exact,
            publicLocation ?? Public,
            timeZoneId,
            pricePerNight,
            maxGuests,
            CreatedAt);
    }

    [Fact]
    public void Create_ShouldCreateActiveListingWithTrimmedValues_WhenArgumentsAreValid()
    {
        var listing = CreateListing(
            title: "  Loft  ",
            description: "  Cozy  ",
            countryCode: " ua ",
            region: "  Ivano-Frankivsk Oblast  ",
            city: " Vorokhta ",
            address: " Danyla Halytskoho 1 ");

        listing.Id.ShouldNotBe(Guid.Empty);
        listing.HostId.ShouldBe(HostId);
        listing.IsActive.ShouldBeTrue();
        listing.Title.ShouldBe("Loft");
        listing.Description.ShouldBe("Cozy");
        listing.CountryCode.ShouldBe("UA");
        listing.Region.ShouldBe("Ivano-Frankivsk Oblast");
        listing.City.ShouldBe("Vorokhta");
        listing.Address.ShouldBe("Danyla Halytskoho 1");
        listing.Location.ShouldBe(Exact);
        listing.PublicLocation.ShouldBe(Public);
        listing.CreatedAtUtc.ShouldBe(CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_ShouldStoreNullDescription_WhenDescriptionIsBlank(string? description)
    {
        var listing = CreateListing(description: description);

        listing.Description.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldThrow_WhenHostIdIsEmpty()
    {
        Should.Throw<ArgumentException>(() => CreateListing(hostId: Guid.Empty));
    }

    [Fact]
    public void Create_ShouldReportHostIdAsParamName_WhenHostIdIsEmpty()
    {
        var exception = Should.Throw<ArgumentException>(() => CreateListing(hostId: Guid.Empty));

        exception.ParamName.ShouldBe("hostId");
    }

    [Fact]
    public void Create_ShouldThrowArgumentNull_WhenTitleIsNull()
    {
        var exception = Should.Throw<ArgumentNullException>(() => CreateListing(title: null!));

        exception.ParamName.ShouldBe("title");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldThrow_WhenPriceIsNotPositive(int price)
    {
        Should.Throw<ArgumentException>(() => CreateListing(pricePerNight: price));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldThrow_WhenMaxGuestsIsNotPositive(int maxGuests)
    {
        Should.Throw<ArgumentException>(() => CreateListing(maxGuests: maxGuests));
    }

    [Fact]
    public void Create_ShouldSucceed_WhenMaxGuestsIsOne()
    {
        var listing = CreateListing(maxGuests: 1);

        listing.MaxGuests.ShouldBe(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenCountryCodeIsMissing(string? countryCode)
    {
        var exception = Should.Throw<ArgumentException>(() => CreateListing(countryCode: countryCode!));

        exception.ParamName.ShouldBe("countryCode");
    }

    [Theory]
    [InlineData("U")]
    [InlineData("UKR")]
    [InlineData("U1")]
    [InlineData("УК")]
    [InlineData("U A")]
    public void Create_ShouldThrow_WhenCountryCodeHasInvalidFormat(string countryCode)
    {
        var exception = Should.Throw<ArgumentException>(() => CreateListing(countryCode: countryCode));

        exception.ParamName.ShouldBe("countryCode");
    }

    [Theory]
    [InlineData("ua", "UA")]
    [InlineData(" pl ", "PL")]
    [InlineData("Us", "US")]
    public void Create_ShouldNormalizeCountryCodeToUpperCase_WhenCountryCodeIsValid(string input, string expected)
    {
        var listing = CreateListing(countryCode: input);

        listing.CountryCode.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldStoreNullRegion_WhenRegionIsBlank(string? region)
    {
        var listing = CreateListing(region: region);

        listing.Region.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldThrowArgumentNull_WhenLocationIsNull()
    {
        var exception = Should.Throw<ArgumentNullException>(() =>
            Listing.Create(HostId, "Loft", null, "UA", null, "Vorokhta", "Address 1",
                null!, Public, Kyiv, 1500m, 2, CreatedAt));

        exception.ParamName.ShouldBe("location");
    }

    [Fact]
    public void Create_ShouldThrowArgumentNull_WhenPublicLocationIsNull()
    {
        var exception = Should.Throw<ArgumentNullException>(() =>
            Listing.Create(HostId, "Loft", null, "UA", null, "Vorokhta", "Address 1",
                Exact, null!, Kyiv, 1500m, 2, CreatedAt));

        exception.ParamName.ShouldBe("publicLocation");
    }

    [Fact]
    public void Create_ShouldStoreTimeZone_WhenTimeZoneIsKnown()
    {
        var listing = CreateListing(timeZoneId: "Asia/Tokyo");

        listing.TimeZoneId.ShouldBe("Asia/Tokyo");
    }

    [Theory]
    [InlineData("")]
    [InlineData("Mars/Olympus_Mons")]
    public void Create_ShouldThrow_WhenTimeZoneIsEmptyOrUnknown(string timeZoneId)
    {
        var exception = Should.Throw<ArgumentException>(() => CreateListing(timeZoneId: timeZoneId));

        exception.ParamName.ShouldBe("timeZoneId");
    }

    [Fact]
    public void Create_ShouldThrow_WhenPublicLocationEqualsExactLocation()
    {
        var exception = Should.Throw<ArgumentException>(() =>
            CreateListing(location: Exact, publicLocation: new GeoPoint(Exact.Latitude, Exact.Longitude)));

        exception.ParamName.ShouldBe("publicLocation");
    }

    [Fact]
    public void Update_ShouldKeepOldValues_WhenPriceIsInvalid()
    {
        var listing = CreateListing();

        Should.Throw<ArgumentException>(() =>
            listing.Update("New title", null, "PL", "Lesser Poland", "Krakow", "Rynek 1",
                pricePerNight: 0m, maxGuests: 4));

        listing.Title.ShouldBe("Loft");
        listing.CountryCode.ShouldBe("UA");
        listing.Region.ShouldBe("Ivano-Frankivsk Oblast");
        listing.City.ShouldBe("Vorokhta");
        listing.PricePerNight.ShouldBe(1500m);
    }

    [Fact]
    public void Update_ShouldKeepOldValues_WhenCountryCodeHasInvalidFormat()
    {
        var listing = CreateListing();

        Should.Throw<ArgumentException>(() =>
            listing.Update("New title", null, "POL", null, "Krakow", "Rynek 1", 2000m, 4));

        listing.Title.ShouldBe("Loft");
        listing.CountryCode.ShouldBe("UA");
    }

    [Fact]
    public void Update_ShouldChangeLabelFieldsAndKeepLocation_WhenArgumentsAreValid()
    {
        var listing = CreateListing();

        listing.Update("Studio", null, "pl", null, "Krakow", "Rynek 1", 2000m, 3);

        listing.CountryCode.ShouldBe("PL");
        listing.Region.ShouldBeNull();
        listing.City.ShouldBe("Krakow");
        listing.Location.ShouldBe(Exact);
        listing.PublicLocation.ShouldBe(Public);
    }

    [Fact]
    public void SetLocation_ShouldReplaceBothPoints_WhenPointsAreValid()
    {
        var listing = CreateListing();
        var newExact = new GeoPoint(50.0614, 19.9366);
        var newPublic = new GeoPoint(50.0640, 19.9400);

        listing.SetLocation(newExact, newPublic, "Europe/Warsaw");

        listing.Location.ShouldBe(newExact);
        listing.PublicLocation.ShouldBe(newPublic);
        listing.TimeZoneId.ShouldBe("Europe/Warsaw");
    }

    [Fact]
    public void SetLocation_ShouldKeepOldPoints_WhenPublicLocationEqualsExactLocation()
    {
        var listing = CreateListing();
        var newExact = new GeoPoint(50.0614, 19.9366);

        Should.Throw<ArgumentException>(() => listing.SetLocation(newExact, newExact, "Europe/Warsaw"));

        listing.Location.ShouldBe(Exact);
        listing.PublicLocation.ShouldBe(Public);
        listing.TimeZoneId.ShouldBe(Kyiv);
    }

    [Fact]
    public void Deactivate_ShouldMakeListingInactive_WhenListingIsActive()
    {
        var listing = CreateListing();

        listing.Deactivate();

        listing.IsActive.ShouldBeFalse();
    }
}
