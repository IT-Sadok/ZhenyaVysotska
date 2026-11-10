using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BookingWeb.Application.Listings.Requests;
using BookingWeb.Domain;
using BookingWeb.IntegrationTests.Infrastructure;
using Shouldly;

namespace BookingWeb.IntegrationTests.Api;

public sealed class ListingPrivacyTests : ApiTestBase
{
    private const string Address = "Secret street 42, apt. 7";
    private const string KyivBox = "minLat=50.3&minLng=30.3&maxLat=50.6&maxLng=30.8";

    public ListingPrivacyTests(ApiFactory factory) : base(factory)
    {
    }

    private async Task<Guid> CreateListingWithKnownAddressAsync(HttpClient host)
    {
        var request = new ListingRequest(
            "Loft", "Cozy loft", "Kyiv", "Kyiv", Address, KyivLatitude, KyivLongitude, 1000m, 4);
        var response = await host.PostAsJsonAsync("/api/listings", request);
        await response.ShouldHaveStatusAsync(HttpStatusCode.Created);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<string> GetBodyAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        return await response.Content.ReadAsStringAsync();
    }
    
    private static (List<string> Names, List<JsonElement> Values) Flatten(string body)
    {
        var names = new List<string>();
        var values = new List<JsonElement>();

        void Walk(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        names.Add(property.Name);
                        Walk(property.Value);
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        Walk(item);
                    }
                    break;
                default:
                    values.Add(element.Clone());
                    break;
            }
        }

        using var document = JsonDocument.Parse(body);
        Walk(document.RootElement);
        return (names, values);
    }

    private static void ShouldNotLeakPrivateData(string body)
    {
        var (names, values) = Flatten(body);

        names.ShouldNotContain(name => name.Contains("address", StringComparison.OrdinalIgnoreCase));
        body.ShouldNotContain(Address);
        
        var numbers = values.Where(v => v.ValueKind == JsonValueKind.Number).Select(v => v.GetDouble()).ToList();
        numbers.ShouldNotContain(KyivLatitude);
        numbers.ShouldNotContain(KyivLongitude);

        names.ShouldContain(name => name.Equals("latitude", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetById_ShouldNotExposeAddressOrExactCoordinates_WhenRequestIsAnonymous()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var id = await CreateListingWithKnownAddressAsync(host);

        ShouldNotLeakPrivateData(await GetBodyAsync(Client, $"/api/listings/{id}"));
    }

    [Fact]
    public async Task Search_ShouldNotExposeAddressOrExactCoordinates_WhenListingsAreReturned()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        await CreateListingWithKnownAddressAsync(host);

        ShouldNotLeakPrivateData(await GetBodyAsync(Client, "/api/listings"));
        ShouldNotLeakPrivateData(await GetBodyAsync(Client, $"/api/listings?{KyivBox}"));
    }

    [Fact]
    public async Task Map_ShouldNotExposeAddressOrExactCoordinates_WhenMarkersAreReturned()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        await CreateListingWithKnownAddressAsync(host);

        ShouldNotLeakPrivateData(await GetBodyAsync(Client, $"/api/listings/map?{KyivBox}"));
    }

    [Fact]
    public async Task PublicEndpoints_ShouldNotExposeExactData_WhenRequestedByOwningHost()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var id = await CreateListingWithKnownAddressAsync(host);

        ShouldNotLeakPrivateData(await GetBodyAsync(host, $"/api/listings/{id}"));
        ShouldNotLeakPrivateData(await GetBodyAsync(host, $"/api/listings?{KyivBox}"));
        ShouldNotLeakPrivateData(await GetBodyAsync(host, $"/api/listings/map?{KyivBox}"));
    }

    [Fact]
    public async Task Mine_ShouldExposeAddressAndExactCoordinates_WhenHostRequestsOwnListings()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        await CreateListingWithKnownAddressAsync(host);

        var body = await GetBodyAsync(host, "/api/listings/mine");

        using var document = JsonDocument.Parse(body);
        var listing = document.RootElement.EnumerateArray().ShouldHaveSingleItem();
        listing.GetProperty("address").GetString().ShouldBe(Address);
        listing.GetProperty("latitude").GetDouble().ShouldBe(KyivLatitude);
        listing.GetProperty("longitude").GetDouble().ShouldBe(KyivLongitude);
        listing.GetProperty("publicLatitude").GetDouble().ShouldNotBe(KyivLatitude);
    }

    [Fact]
    public async Task Mine_ShouldReturnUnauthorized_WhenRequestIsAnonymous()
    {
        var response = await Client.GetAsync("/api/listings/mine");

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }
}
