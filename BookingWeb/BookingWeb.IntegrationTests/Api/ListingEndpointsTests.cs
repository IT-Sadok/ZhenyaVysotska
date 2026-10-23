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
        return new ListingRequest("Loft", "Cozy loft", "Kyiv", "Khreshchatyk 1", price, 4);
    }

    private async Task<PagedResult<ListingDto>> SearchAsync(string query = "")
    {
        var response = await Client.GetAsync($"/api/listings{query}");
        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<PagedResult<ListingDto>>())!;
    }

    [Fact]
    public async Task CreateListing_ShouldReturnCreatedWithLocation_WhenUserIsHost()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);

        var response = await host.PostAsJsonAsync("/api/listings", CreateRequest());

        await response.ShouldHaveStatusAsync(HttpStatusCode.Created);
        
        var listing = (await response.Content.ReadFromJsonAsync<ListingDto>())!;
        
        response.Headers.Location!.ToString().ShouldBe($"/api/listings/{listing.Id}");
        
        listing.IsActive.ShouldBeTrue();
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
        (await response.Content.ReadFromJsonAsync<ListingDto>())!.Title.ShouldBe("Loft");
    }

    [Fact]
    public async Task GetMyListings_ShouldReturnOnlyOwnListings_WhenHostHasListings()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        using var otherHost = await CreateUserClientAsync("other-host@test.com", Roles.Host);
        var own = await CreateListingAsync(host);
        await CreateListingAsync(otherHost);

        var listings = await host.GetFromJsonAsync<List<ListingDto>>("/api/listings/mine");

        listings!.Select(listing => listing.Id).ShouldBe(new[] { own.Id });
    }

    [Fact]
    public async Task UpdateListing_ShouldPersistChanges_WhenHostOwnsListing()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var listing = await CreateListingAsync(host);
        var update = new ListingRequest("Sea view", null, "Odesa", "Deribasivska 1", 2500m, 6);

        var response = await host.PutAsJsonAsync($"/api/listings/{listing.Id}", update);

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        var stored = await Client.GetFromJsonAsync<ListingDto>($"/api/listings/{listing.Id}");
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
        var stored = await Client.GetFromJsonAsync<ListingDto>($"/api/listings/{listing.Id}");
        stored!.PricePerNight.ShouldBe(1000m);
    }

    [Fact]
    public async Task DeleteListing_ShouldHideListingFromPublic_WhenThereAreNoUpcomingBookings()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var listing = await CreateListingAsync(host);

        var response = await host.DeleteAsync($"/api/listings/{listing.Id}");

        await response.ShouldHaveStatusAsync(HttpStatusCode.NoContent);
        await (await Client.GetAsync($"/api/listings/{listing.Id}")).ShouldHaveStatusAsync(HttpStatusCode.NotFound);
        (await SearchAsync()).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task SearchListings_ShouldFilterByCityIgnoringCase_WhenCityIsGiven()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var kyiv = await CreateListingAsync(host, city: "Kyiv");
        await CreateListingAsync(host, city: "Lviv");

        var page = await SearchAsync("?city=kyiv");

        page.Items.Select(listing => listing.Id).ShouldBe(new[] { kyiv.Id });
    }
    
    [Fact]
    public async Task SearchListings_ShouldFilterByCityIgnoringCaseInCyrillic_WhenCityIsGiven()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var kyiv = await CreateListingAsync(host, city: "Київ");
        await CreateListingAsync(host, city: "Львів");

        var page = await SearchAsync("?city=київ");

        page.Items.Select(listing => listing.Id).ShouldBe(new[] { kyiv.Id });
    }
}