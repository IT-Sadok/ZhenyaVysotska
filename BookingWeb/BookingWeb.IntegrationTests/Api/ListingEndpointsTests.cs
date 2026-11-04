using System.Net;
using System.Net.Http.Json;
using BookingWeb.Application.Listings;
using BookingWeb.Application.Listings.Requests;
using BookingWeb.Application.Models;
using BookingWeb.Domain;
using BookingWeb.IntegrationTests.Infrastructure;
using Shouldly;

namespace BookingWeb.IntegrationTests.Api;

public sealed class ListingEndpointsTests : ApiTestBase
{
    public ListingEndpointsTests(ApiFactory factory) : base(factory)
    {
        
    }
    
    private static ListingRequest CreateRequest(decimal price = 1000m)
    {
        return new ListingRequest(
            "Loft", "Cozy loft", "UA", "Kyiv", "Kyiv", "Khreshchatyk 1", KyivLatitude, KyivLongitude, price, 4);
    }

    private async Task<PagedResult<PublicListingDto>> SearchAsync(string query = "")
    {
        var response = await Client.GetAsync($"/api/listings{query}");
        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<PagedResult<PublicListingDto>>())!;
    }

    [Fact]
    public async Task CreateListing_ShouldReturnCreatedActiveListingWithLocationHeader_WhenUserIsHost()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);

        var response = await host.PostAsJsonAsync("/api/listings", CreateRequest());

        await response.ShouldHaveStatusAsync(HttpStatusCode.Created);
        
        var listing = (await response.Content.ReadFromJsonAsync<HostListingDto>())!;
        
        response.Headers.Location!.ToString().ShouldBe($"/api/listings/{listing.Id}");
        
        listing.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateListing_ShouldResolveTimeZoneFromCoordinates_WhenUserIsHost()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);

        var listing = await CreateListingAsync(host);

        listing.TimeZoneId.ShouldBe("Europe/Kyiv");
    }

    [Fact]
    public async Task UpdateListing_ShouldResolveTimeZoneAgain_WhenCoordinatesMoveToAnotherZone()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var listing = await CreateListingAsync(host);
        var tokyo = new ListingRequest(
            "Loft", "Cozy loft", "JP", "Tokyo", "Tokyo", "Shibuya 1", 35.6595, 139.7005, 1000m, 4);

        var response = await host.PutAsJsonAsync($"/api/listings/{listing.Id}", tokyo);

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        var updated = (await response.Content.ReadFromJsonAsync<HostListingDto>())!;
        updated.TimeZoneId.ShouldBe("Asia/Tokyo");
    }

    [Fact]
    public async Task CreateListing_ShouldReturnForbidden_WhenUserIsOnlyClient()
    {
        using var client = await CreateUserClientAsync("client@test.com", Roles.Client);

        var response = await client.PostAsJsonAsync("/api/listings", CreateRequest());

        await response.ShouldHaveStatusAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateListing_ShouldReturnUnauthorized_WhenTokenIsMissing()
    {
        var response = await Client.PostAsJsonAsync("/api/listings", CreateRequest());

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateListing_ShouldReturnValidationProblem_WhenPriceIsZero()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);

        var response = await host.PostAsJsonAsync("/api/listings", CreateRequest(price: 0m));

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        (await response.ReadValidationErrorFieldsAsync())
            .ShouldContain(field => field.Equals("PricePerNight", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetListing_ShouldReturnListing_WhenRequestIsAnonymous()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var listing = await CreateListingAsync(host);

        var response = await Client.GetAsync($"/api/listings/{listing.Id}");

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<PublicListingDto>())!.Title.ShouldBe("Loft");
    }

    [Fact]
    public async Task GetMyListings_ShouldReturnOnlyOwnListings_WhenHostHasListings()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        using var otherHost = await CreateUserClientAsync("other-host@test.com", Roles.Host);
        var own = await CreateListingAsync(host);
        await CreateListingAsync(otherHost);

        var page = await host.GetFromJsonAsync<PagedResult<HostListingDto>>("/api/listings/mine");

        page!.Items.Select(listing => listing.Id).ShouldBe(new[] { own.Id });
        page.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task UpdateListing_ShouldPersistChanges_WhenHostOwnsListing()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var listing = await CreateListingAsync(host);
        var update = new ListingRequest(
            "Sea view", null, "UA", "Odesa Oblast", "Odesa", "Deribasivska 1", 46.4825, 30.7233, 2500m, 6);

        var response = await host.PutAsJsonAsync($"/api/listings/{listing.Id}", update);

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        var stored = await Client.GetFromJsonAsync<PublicListingDto>($"/api/listings/{listing.Id}");
        stored!.City.ShouldBe("Odesa");
        stored.PricePerNight.ShouldBe(2500m);
    }

    [Fact]
    public async Task UpdateListing_ShouldReturnNotFoundAndKeepListing_WhenListingBelongsToOtherHost()
    {
        using var owner = await CreateUserClientAsync("owner@test.com", Roles.Host);
        using var intruder = await CreateUserClientAsync("intruder@test.com", Roles.Host);
        var listing = await CreateListingAsync(owner);

        var response = await intruder.PutAsJsonAsync($"/api/listings/{listing.Id}", CreateRequest(price: 1m));

        await response.ShouldHaveStatusAsync(HttpStatusCode.NotFound);
        var stored = await Client.GetFromJsonAsync<PublicListingDto>($"/api/listings/{listing.Id}");
        stored!.PricePerNight.ShouldBe(1000m);
    }

    [Fact]
    public async Task DeleteListing_ShouldHideListingFromPublic_WhenHostOwnsListing()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var listing = await CreateListingAsync(host);

        var response = await host.DeleteAsync($"/api/listings/{listing.Id}");

        await response.ShouldHaveStatusAsync(HttpStatusCode.NoContent);
        await (await Client.GetAsync($"/api/listings/{listing.Id}")).ShouldHaveStatusAsync(HttpStatusCode.NotFound);
        (await SearchAsync()).TotalCount.ShouldBe(0);
    }

    private static Dictionary<string, object?> RawRequest(params string[] omit)
    {
        var body = new Dictionary<string, object?>
        {
            ["title"] = "Loft",
            ["description"] = "Cozy loft",
            ["countryCode"] = "UA",
            ["region"] = "Kyiv",
            ["city"] = "Kyiv",
            ["address"] = "Khreshchatyk 1",
            ["latitude"] = KyivLatitude,
            ["longitude"] = KyivLongitude,
            ["pricePerNight"] = 1000m,
            ["maxGuests"] = 4,
        };
        foreach (var key in omit)
        {
            body.Remove(key);
        }

        return body;
    }

    [Theory]
    [InlineData("latitude")]
    [InlineData("longitude")]
    public async Task CreateListing_ShouldReturnLocationRequiredAndNotCreateListing_WhenCoordinateIsMissing(string missing)
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);

        var response = await host.PostAsJsonAsync("/api/listings", RawRequest(omit: missing));

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        (await response.ReadErrorCodesAsync()).ShouldContain(ListingErrors.LocationRequired.Code);
        (await SearchAsync()).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task CreateListing_ShouldReturnInvalidCountryCode_WhenCountryCodeIsMissing()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);

        var response = await host.PostAsJsonAsync("/api/listings", RawRequest(omit: "countryCode"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        (await response.ReadErrorCodesAsync()).ShouldContain(ListingErrors.InvalidCountryCode.Code);
    }

    [Theory]
    [InlineData("XX")]
    [InlineData("UK")]
    [InlineData("UKR")]
    public async Task CreateListing_ShouldReturnInvalidCountryCode_WhenCodeIsNotIsoAlpha2(string countryCode)
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var request = RawRequest();
        request["countryCode"] = countryCode;

        var response = await host.PostAsJsonAsync("/api/listings", request);

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        (await response.ReadErrorCodesAsync()).ShouldContain(ListingErrors.InvalidCountryCode.Code);
    }

    [Fact]
    public async Task CreateListing_ShouldSucceedWithNullRegion_WhenRegionIsMissing()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);

        var response = await host.PostAsJsonAsync("/api/listings", RawRequest(omit: "region"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.Created);
        var listing = (await response.Content.ReadFromJsonAsync<HostListingDto>())!;
        listing.Region.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateListing_ShouldReturnLocationRequiredAndKeepListing_WhenLatitudeIsMissing()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var listing = await CreateListingAsync(host);

        var response = await host.PutAsJsonAsync($"/api/listings/{listing.Id}", RawRequest(omit: "latitude"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        (await response.ReadErrorCodesAsync()).ShouldContain(ListingErrors.LocationRequired.Code);
        var stored = await Client.GetFromJsonAsync<PublicListingDto>($"/api/listings/{listing.Id}");
        (stored!.Latitude, stored.Longitude).ShouldBe((listing.PublicLatitude, listing.PublicLongitude));
    }
}
