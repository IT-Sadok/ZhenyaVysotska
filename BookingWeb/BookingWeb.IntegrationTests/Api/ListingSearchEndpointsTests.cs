using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using BookingWeb.Application.Bookings;
using BookingWeb.Application.Listings;
using BookingWeb.Application.Listings.Requests;
using BookingWeb.Application.Models;
using BookingWeb.Domain;
using BookingWeb.IntegrationTests.Infrastructure;
using Shouldly;

namespace BookingWeb.IntegrationTests.Api;

public sealed class ListingSearchEndpointsTests : ApiTestBase
{
    private const string KyivBox = "minLat=50.3&minLng=30.3&maxLat=50.6&maxLng=30.8";

    private const double LvivLatitude = 49.8397;
    private const double LvivLongitude = 24.0297;

    public ListingSearchEndpointsTests(ApiFactory factory) : base(factory)
    {
        
    }

    private static string Inv(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private async Task<MapMarkersDto> GetMapAsync(string query)
    {
        var response = await Client.GetAsync($"/api/listings/map?{query}");
        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<MapMarkersDto>())!;
    }

    private async Task<PagedResult<PublicListingDto>> GetListAsync(string query)
    {
        var response = await Client.GetAsync($"/api/listings?{query}");
        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<PagedResult<PublicListingDto>>())!;
    }

    private async Task<List<Guid>> GetAllListIdsAsync(string query, int pageSize)
    {
        var ids = new List<Guid>();
        for (var page = 1; ; page++)
        {
            var result = await GetListAsync($"{query}&page={page}&pageSize={pageSize}");
            ids.AddRange(result.Items.Select(item => item.Id));
            if (page * pageSize >= result.TotalCount)
            {
                return ids;
            }
        }
    }

    private async Task<HostListingDto> GetMineAsync(HttpClient host, Guid listingId)
    {
        var mine = await host.GetFromJsonAsync<PagedResult<HostListingDto>>("/api/listings/mine");
        return mine!.Items.Single(listing => listing.Id == listingId);
    }


    [Fact]
    public async Task Map_ShouldReturnOnlyListingsInsideBox_WhenOthersAreOutside()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var kyiv = await CreateListingAsync(host);
        await CreateListingAsync(host, latitude: LvivLatitude, longitude: LvivLongitude);

        var map = await GetMapAsync(KyivBox);

        map.Markers.Select(marker => marker.Id).ShouldBe([kyiv.Id]);
    }

    [Fact]
    public async Task List_ShouldReturnOnlyListingsInsideBox_WhenBoxIsGiven()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var kyiv = await CreateListingAsync(host);
        await CreateListingAsync(host, latitude: LvivLatitude, longitude: LvivLongitude);

        var page = await GetListAsync(KyivBox);

        page.Items.Select(item => item.Id).ShouldBe([kyiv.Id]);
        page.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task MapAndList_ShouldNotReturnListing_WhenListingIsInactive()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var listing = await CreateListingAsync(host);
        await (await host.DeleteAsync($"/api/listings/{listing.Id}")).ShouldHaveStatusAsync(HttpStatusCode.NoContent);

        (await GetMapAsync(KyivBox)).Markers.ShouldBeEmpty();
        (await GetListAsync(KyivBox)).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Map_ShouldFindListingByPublicPoint_WhenPointLiesExactlyOnBoxEdge()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var created = await CreateListingAsync(host);
        var publicLat = created.PublicLatitude;
        var publicLng = created.PublicLongitude;

        var map = await GetMapAsync(
            $"minLat={Inv(publicLat)}&minLng={Inv(publicLng)}&maxLat={Inv(publicLat + 0.1)}&maxLng={Inv(publicLng + 0.1)}");

        map.Markers.Select(marker => marker.Id).ShouldBe([created.Id]);
    }

    [Fact]
    public async Task Map_ShouldNotReturnListing_WhenBoxContainsExactButNotPublicPoint()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var created = await CreateListingAsync(host);
        
        const double tenMetres = 0.0001;
        var map = await GetMapAsync(
            $"minLat={Inv(KyivLatitude - tenMetres)}&minLng={Inv(KyivLongitude - tenMetres)}" +
            $"&maxLat={Inv(KyivLatitude + tenMetres)}&maxLng={Inv(KyivLongitude + tenMetres)}");

        map.Markers.ShouldNotContain(marker => marker.Id == created.Id);
    }

    [Fact]
    public async Task Map_ShouldReturnPublicPoint200To500MetersFromExact_WhenListingIsCreated()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        await CreateListingAsync(host);

        var marker = (await GetMapAsync(KyivBox)).Markers.ShouldHaveSingleItem();

        var distance = GeoDistance.Meters(KyivLatitude, KyivLongitude, marker.Latitude, marker.Longitude);
        distance.ShouldBeInRange(200 - 0.01, 500 + 0.01);
    }

    [Fact]
    public async Task PublicEndpoints_ShouldReturnSamePublicPoint_WhenRequestedRepeatedly()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var created = await CreateListingAsync(host);

        var points = new HashSet<(double, double)>();
        for (var i = 0; i < 5; i++)
        {
            var marker = (await GetMapAsync(KyivBox)).Markers.Single();
            var item = (await GetListAsync(KyivBox)).Items.Single();
            var details = (await Client.GetFromJsonAsync<PublicListingDto>($"/api/listings/{created.Id}"))!;

            points.Add((marker.Latitude, marker.Longitude));
            points.Add((item.Latitude, item.Longitude));
            points.Add((details.Latitude, details.Longitude));
        }
        
        points.ShouldBe([(created.PublicLatitude, created.PublicLongitude)]);
    }

    [Fact]
    public async Task Update_ShouldMovePublicPointNearNewLocation_WhenHostChangesCoordinates()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var created = await CreateListingAsync(host);
        var update = new ListingRequest(
            "Loft", "Cozy loft", "UA", "Lviv Oblast", "Lviv", "Rynok 1", LvivLatitude, LvivLongitude, 1000m, 4);

        await (await host.PutAsJsonAsync($"/api/listings/{created.Id}", update)).ShouldHaveStatusAsync(HttpStatusCode.OK);

        var details = (await Client.GetFromJsonAsync<PublicListingDto>($"/api/listings/{created.Id}"))!;
        GeoDistance.Meters(LvivLatitude, LvivLongitude, details.Latitude, details.Longitude)
            .ShouldBeInRange(200 - 0.01, 500 + 0.01);
        (await GetMapAsync(KyivBox)).Markers.ShouldBeEmpty();
    }

    [Fact]
    public async Task Update_ShouldKeepPublicPoint_WhenHostChangesOnlyOtherFields()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var created = await CreateListingAsync(host);
        var update = new ListingRequest(
            "Renamed", null, "UA", "Kyiv", "Kyiv", "Khreshchatyk 1", KyivLatitude, KyivLongitude, 1200m, 4);

        await (await host.PutAsJsonAsync($"/api/listings/{created.Id}", update)).ShouldHaveStatusAsync(HttpStatusCode.OK);

        var mine = await GetMineAsync(host, created.Id);
        (mine.PublicLatitude, mine.PublicLongitude).ShouldBe((created.PublicLatitude, created.PublicLongitude));
    }
    

    [Fact]
    public async Task Map_ShouldReturnAtMostLimitNewestFirstAndMarkTruncated_WhenMoreListingsMatch()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var created = new List<HostListingDto>();
        for (var i = 0; i < 3; i++)
        {
            created.Add(await CreateListingAsync(host));
        }

        var limited = await GetMapAsync($"{KyivBox}&limit=2");
        var all = await GetMapAsync($"{KyivBox}&limit=3");

        limited.Markers.Count.ShouldBe(2);
        limited.IsTruncated.ShouldBeTrue();
        limited.Markers.Select(m => m.Id).ShouldBe(created.OrderByDescending(c => c.CreatedAtUtc).ThenBy(c => c.Id).Take(2).Select(c => c.Id));
        all.Markers.Count.ShouldBe(3);
        all.IsTruncated.ShouldBeFalse();
    }

    [Theory]
    [InlineData("minLat=50.6&minLng=30.3&maxLat=50.3&maxLng=30.8")]   // min > max
    [InlineData("minLat=40&minLng=20&maxLat=55&maxLng=30")]           // 15° > 10° span
    [InlineData("minLat=50.3&minLng=170&maxLat=50.6&maxLng=-170")]    // crosses the 180th meridian
    [InlineData("minLat=-91&minLng=30.3&maxLat=-85&maxLng=30.8")]     // out of range
    [InlineData("minLat=50.3&minLng=30.3&maxLat=50.6")]               // maxLng missing
    [InlineData("")]                                                  // no box at all
    public async Task Map_ShouldReturnInvalidBoundingBox_WhenBoxIsInvalid(string query)
    {
        var response = await Client.GetAsync($"/api/listings/map?{query}");

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        (await response.ReadErrorCodesAsync()).ShouldContain(ListingErrors.InvalidBoundingBox.Code);
    }

    [Theory]
    [InlineData("minLat=50.3")]
    [InlineData("minLat=50.3&minLng=30.3&maxLat=50.6")]
    [InlineData("maxLng=30.8")]
    public async Task List_ShouldReturnInvalidBoundingBox_WhenOnlySomeBoxValuesAreGiven(string query)
    {
        var response = await Client.GetAsync($"/api/listings?{query}");

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        (await response.ReadErrorCodesAsync()).ShouldContain(ListingErrors.InvalidBoundingBox.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public async Task Map_ShouldReturnValidationProblem_WhenLimitIsOutOfRange(int limit)
    {
        var response = await Client.GetAsync($"/api/listings/map?{KyivBox}&limit={limit}");

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        (await response.ReadValidationErrorFieldsAsync())
            .ShouldContain(field => field.Equals("Limit", StringComparison.OrdinalIgnoreCase));
    }
    
    [Fact]
    public async Task List_ShouldReturnActiveListingsMatchingOtherFilters_WhenNoBoxIsGiven()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var match = await CreateListingAsync(host, pricePerNight: 1500m, maxGuests: 4);
        var lvivMatch = await CreateListingAsync(host, pricePerNight: 2000m, maxGuests: 6,
            latitude: LvivLatitude, longitude: LvivLongitude);
        await CreateListingAsync(host, pricePerNight: 500m, maxGuests: 4);   
        await CreateListingAsync(host, pricePerNight: 1500m, maxGuests: 2); 
        var inactive = await CreateListingAsync(host, pricePerNight: 1500m, maxGuests: 4);
        await host.DeleteAsync($"/api/listings/{inactive.Id}");

        var page = await GetListAsync("minPrice=1000&maxPrice=2000&guests=3");

        page.Items.Select(item => item.Id).ShouldBe([match.Id, lvivMatch.Id], ignoreOrder: true);
    }

    [Fact]
    public async Task MapAndList_ShouldReturnSameListings_WhenFiltersAreTheSame()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        await CreateListingAsync(host, pricePerNight: 1000m, maxGuests: 4);
        await CreateListingAsync(host, pricePerNight: 1500m, maxGuests: 6);
        await CreateListingAsync(host, pricePerNight: 2000m, maxGuests: 3);
        await CreateListingAsync(host, pricePerNight: 2500m, maxGuests: 4);  // too expensive
        await CreateListingAsync(host, pricePerNight: 900m, maxGuests: 4);   // too cheap
        await CreateListingAsync(host, pricePerNight: 1200m, maxGuests: 2);  // too few guests
        await CreateListingAsync(host, pricePerNight: 1200m, maxGuests: 4,
            latitude: LvivLatitude, longitude: LvivLongitude);                // outside the box
        var inactive = await CreateListingAsync(host, pricePerNight: 1200m, maxGuests: 4);
        await host.DeleteAsync($"/api/listings/{inactive.Id}");
        await CreateListingAsync(host, pricePerNight: 1800m, maxGuests: 5);

        const string filters = $"{KyivBox}&minPrice=1000&maxPrice=2000&guests=3";

        var mapIds = (await GetMapAsync($"{filters}&limit=200")).Markers.Select(m => m.Id).ToList();
        var listIds = await GetAllListIdsAsync(filters, pageSize: 2);

        mapIds.Count.ShouldBe(4);
        mapIds.ShouldBe(listIds);
    }

    private static string Stay(DateOnly checkIn, DateOnly checkOut) =>
        $"checkIn={checkIn:yyyy-MM-dd}&checkOut={checkOut:yyyy-MM-dd}";

    [Fact]
    public async Task MapAndList_ShouldExcludeListing_WhenRequestedStayOverlapsConfirmedBooking()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var booked = await CreateListingAsync(host);
        var free = await CreateListingAsync(host);
        using var guest = await CreateUserClientAsync("guest@test.com", Roles.Client);
        (await BookAsync(guest, booked.Id, Today.AddDays(10), Today.AddDays(13))).EnsureSuccessStatusCode();

        var query = $"{KyivBox}&{Stay(Today.AddDays(12), Today.AddDays(15))}";
        var mapIds = (await GetMapAsync(query)).Markers.Select(m => m.Id).ToList();
        var listIds = (await GetListAsync(query)).Items.Select(l => l.Id).ToList();

        mapIds.ShouldBe([free.Id]);
        listIds.ShouldBe([free.Id]);
    }

    [Fact]
    public async Task MapAndList_ShouldIncludeListing_WhenStayStartsOnBookingCheckOutDay()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var listing = await CreateListingAsync(host);
        using var guest = await CreateUserClientAsync("guest@test.com", Roles.Client);
        (await BookAsync(guest, listing.Id, Today.AddDays(10), Today.AddDays(13))).EnsureSuccessStatusCode();

        var query = $"{KyivBox}&{Stay(Today.AddDays(13), Today.AddDays(15))}";

        (await GetMapAsync(query)).Markers.Select(m => m.Id).ShouldBe([listing.Id]);
        (await GetListAsync(query)).Items.Select(l => l.Id).ShouldBe([listing.Id]);
    }

    [Fact]
    public async Task MapAndList_ShouldIncludeListing_WhenOverlappingBookingIsCancelled()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var listing = await CreateListingAsync(host);
        using var guest = await CreateUserClientAsync("guest@test.com", Roles.Client);
        var bookResponse = await BookAsync(guest, listing.Id, Today.AddDays(10), Today.AddDays(13));
        var booking = (await bookResponse.Content.ReadFromJsonAsync<BookingDto>())!;
        (await guest.PostAsync($"/api/bookings/{booking.Id}/cancel", null)).EnsureSuccessStatusCode();

        var query = $"{KyivBox}&{Stay(Today.AddDays(10), Today.AddDays(13))}";

        (await GetMapAsync(query)).Markers.Select(m => m.Id).ShouldBe([listing.Id]);
        (await GetListAsync(query)).Items.Select(l => l.Id).ShouldBe([listing.Id]);
    }

    [Fact]
    public async Task Map_ShouldReturnValidationProblem_WhenCheckInIsInThePast()
    {
        var response = await Client.GetAsync(
            $"/api/listings/map?{KyivBox}&{Stay(Today.AddDays(-1), Today.AddDays(2))}");

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
    }
}
