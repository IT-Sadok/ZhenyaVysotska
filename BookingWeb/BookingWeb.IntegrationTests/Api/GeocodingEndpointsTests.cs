using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookingWeb.Api.RateLimiting;
using BookingWeb.Application.Geocoding;
using BookingWeb.Domain;
using BookingWeb.IntegrationTests.Infrastructure;
using Shouldly;

namespace BookingWeb.IntegrationTests.Api;

public sealed class GeocodingEndpointsTests : ApiTestBase
{
    private const string SearchUrl = "/api/geocoding/search";
    private const string ReverseUrl = "/api/geocoding/reverse";

    public GeocodingEndpointsTests(ApiFactory factory) : base(factory)
    {
    }

    private FakeNominatimHandler Nominatim => Factory.Nominatim;

    private Task<HttpClient> CreateHostAsync() => CreateUserClientAsync("host@test.com", Roles.Host);

    private static string Search(string query) => $"{SearchUrl}?q={Uri.EscapeDataString(query)}";

    private static IPAddress NewIp() => new(Guid.NewGuid().ToByteArray());

    private HttpClient AuthorizedClientFrom(string accessToken, IPAddress ip)
    {
        var client = CreateClient(ip);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
    
    private static async Task ExhaustSearchLimitAsync(HttpClient client)
    {
        for (var i = 0; i < 11; i++)
        {
            await client.GetAsync(Search("Хрещатик, Київ"));
        }
    }

    [Fact]
    public async Task Search_ShouldReturnCandidatesWithBoundingBoxInMinMaxOrder_WhenNominatimFindsPlaces()
    {
        using var host = await CreateHostAsync();

        var response = await host.GetAsync(Search("Хрещатик, Київ"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        var candidate = (await response.Content.ReadFromJsonAsync<List<GeocodingCandidateDto>>())!.ShouldHaveSingleItem();
        candidate.DisplayName.ShouldBe("Хрещатик, Київ, Україна");
        candidate.Latitude.ShouldBe(50.4473);
        candidate.Longitude.ShouldBe(30.5225);
        candidate.BoundingBox.ShouldBe(new GeocodingBoundingBoxDto(50.44, 30.52, 50.45, 30.53));
    }

    [Fact]
    public async Task Search_ShouldSendUserAgentFormatAndNormalizedQuery_WhenCalled()
    {
        using var host = await CreateHostAsync();

        await host.GetAsync(Search("  Хрещатик,   Київ "));

        var sent = Nominatim.Requests.ShouldHaveSingleItem();
        sent.UserAgent.ShouldBe("BookingWeb/1.0 (learning project)");
        sent.Uri.AbsolutePath.ShouldBe("/search");
        var query = System.Web.HttpUtility.ParseQueryString(sent.Uri.Query);
        query["q"].ShouldBe("Хрещатик, Київ");
        query["format"].ShouldBe("jsonv2");
        query["accept-language"].ShouldBe("uk,en");
    }

    [Fact]
    public async Task Search_ShouldRestrictResultsToConfiguredCountries_WhenCalled()
    {
        using var host = await CreateHostAsync();

        await host.GetAsync(Search("Одеса"));

        var sent = Nominatim.Requests.ShouldHaveSingleItem();
        var query = System.Web.HttpUtility.ParseQueryString(sent.Uri.Query);
        query["countrycodes"].ShouldBe("ua");
    }

    [Fact]
    public async Task Search_ShouldServeRepeatedQueryFromCache_WhenQueryDiffersOnlyByCaseAndWhitespace()
    {
        using var host = await CreateHostAsync();
        var first = await host.GetAsync(Search("Хрещатик, Київ"));
        
        var second = await host.GetAsync(Search("  хрещатик,  КИЇВ  "));

        await second.ShouldHaveStatusAsync(HttpStatusCode.OK);
        (await second.Content.ReadAsStringAsync()).ShouldBe(await first.Content.ReadAsStringAsync());
        Nominatim.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Search_ShouldReturnEmptyList_WhenNothingIsFound()
    {
        using var host = await CreateHostAsync();
        Nominatim.RespondWithJson("[]");

        var response = await host.GetAsync(Search("Неіснуюча вулиця, Нікуди"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<List<GeocodingCandidateDto>>())!.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("   ab   ")]
    public async Task Search_ShouldReturn400AndNotCallNominatim_WhenQueryIsTooShort(string query)
    {
        using var host = await CreateHostAsync();

        var response = await host.GetAsync(Search(query));

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        Nominatim.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Search_ShouldReturn400_WhenQueryIsMissing()
    {
        using var host = await CreateHostAsync();

        var response = await host.GetAsync(SearchUrl);

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_ShouldReturn503AndNotCacheTheFailure_WhenNominatimReturnsServerError()
    {
        using var host = await CreateHostAsync();
        Nominatim.RespondWithStatus(HttpStatusCode.InternalServerError);

        var failed = await host.GetAsync(Search("Хрещатик, Київ"));

        await failed.ShouldHaveStatusAsync(HttpStatusCode.ServiceUnavailable);
        (await failed.ReadProblemTitleAsync()).ShouldBe(GeocodingErrors.Unavailable.Code);

        Nominatim.Reset();
        Factory.ReplenishNominatimLimit();
        var retried = await host.GetAsync(Search("Хрещатик, Київ"));

        await retried.ShouldHaveStatusAsync(HttpStatusCode.OK);
        Nominatim.Requests.Count.ShouldBe(1);
    }

    public static TheoryData<Exception> TransportFailures => new()
    {
        new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout."),
        new HttpRequestException("Connection refused"),
    };

    [Theory]
    [MemberData(nameof(TransportFailures))]
    public async Task Search_ShouldReturn503_WhenNominatimTimesOutOrIsUnreachable(Exception failure)
    {
        using var host = await CreateHostAsync();
        Nominatim.Throw(failure);

        var response = await host.GetAsync(Search("Хрещатик, Київ"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.ServiceUnavailable);
        (await response.ReadProblemTitleAsync()).ShouldBe(GeocodingErrors.Unavailable.Code);
    }

    public static TheoryData<string> MalformedPlaces => new()
    {
        // boundingbox with 3 items instead of 4
        """[{ "lat": "50.4473", "lon": "30.5225", "display_name": "Київ", "boundingbox": ["50.44", "50.45", "30.52"] }]""",
        // lat is not a number
        """[{ "lat": "abc", "lon": "30.5225", "display_name": "Київ", "boundingbox": ["50.44", "50.45", "30.52", "30.53"] }]""",
        // boundingbox and display_name are missing
        """[{ "lat": "50.4473", "lon": "30.5225" }]""",
    };

    [Theory]
    [MemberData(nameof(MalformedPlaces))]
    public async Task Search_ShouldReturn503AndNotCacheTheFailure_WhenNominatimReturnsMalformedPlace(string json)
    {
        using var host = await CreateHostAsync();
        Nominatim.RespondWithJson(json);

        var failed = await host.GetAsync(Search("Хрещатик, Київ"));

        await failed.ShouldHaveStatusAsync(HttpStatusCode.ServiceUnavailable);
        (await failed.ReadProblemTitleAsync()).ShouldBe(GeocodingErrors.Unavailable.Code);

        Nominatim.Reset();
        Factory.ReplenishNominatimLimit();
        var retried = await host.GetAsync(Search("Хрещатик, Київ"));

        await retried.ShouldHaveStatusAsync(HttpStatusCode.OK);
        Nominatim.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Search_ShouldReturn503Busy_WhenGlobalRateLimitIsExhausted()
    {
        using var host = await CreateHostAsync();
        await host.GetAsync(Search("Хрещатик, Київ"));

        var rejected = await host.GetAsync(Search("Площа Ринок, Львів"));

        await rejected.ShouldHaveStatusAsync(HttpStatusCode.ServiceUnavailable);
        (await rejected.ReadProblemTitleAsync()).ShouldBe(GeocodingErrors.Busy.Code);
        Nominatim.Requests.Count.ShouldBe(1);

        Factory.ReplenishNominatimLimit();
        var afterRefill = await host.GetAsync(Search("Площа Ринок, Львів"));

        await afterRefill.ShouldHaveStatusAsync(HttpStatusCode.OK);
        Nominatim.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Search_ShouldReturn503BusyAndNotCacheTheFailure_WhenNominatimItselfReturns429()
    {
        using var host = await CreateHostAsync();
        Nominatim.RespondWithStatus(HttpStatusCode.TooManyRequests);

        var rejected = await host.GetAsync(Search("Хрещатик, Київ"));

        await rejected.ShouldHaveStatusAsync(HttpStatusCode.ServiceUnavailable);
        (await rejected.ReadProblemTitleAsync()).ShouldBe(GeocodingErrors.Busy.Code);

        Nominatim.Reset();
        Factory.ReplenishNominatimLimit();
        var retried = await host.GetAsync(Search("Хрещатик, Київ"));

        await retried.ShouldHaveStatusAsync(HttpStatusCode.OK);
        Nominatim.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Reverse_ShouldReturnCityAndRegion_WhenAddressIsFound()
    {
        using var host = await CreateHostAsync();

        var response = await host.GetAsync($"{ReverseUrl}?lat=50.4501&lng=30.5234");

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        var address = (await response.Content.ReadFromJsonAsync<ReverseGeocodingDto>())!;
        address.ShouldBe(new ReverseGeocodingDto("Хрещатик, Київ, 01001, Україна", "Київ", "Київ"));
    }

    [Fact]
    public async Task Reverse_ShouldSendCoordinatesInInvariantFormat_WhenCalled()
    {
        using var host = await CreateHostAsync();

        await host.GetAsync($"{ReverseUrl}?lat=50.4501&lng=-0.1278");

        var sent = Nominatim.Requests.ShouldHaveSingleItem();
        sent.Uri.AbsolutePath.ShouldBe("/reverse");
        var query = System.Web.HttpUtility.ParseQueryString(sent.Uri.Query);
        query["lat"].ShouldBe("50.4501");
        query["lon"].ShouldBe("-0.1278");
        query["addressdetails"].ShouldBe("1");
    }

    [Theory]
    [InlineData("town")]
    [InlineData("village")]
    [InlineData("municipality")]
    public async Task Reverse_ShouldUseSmallerSettlementAsCity_WhenCityIsMissing(string key)
    {
        using var host = await CreateHostAsync();
        Nominatim.RespondWithJson($$"""
            { "display_name": "Яремче", "address": { "{{key}}": "Яремче", "state": "Івано-Франківська область", "country_code": "ua" } }
            """);

        var response = await host.GetAsync($"{ReverseUrl}?lat=48.45&lng=24.55");

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        var address = (await response.Content.ReadFromJsonAsync<ReverseGeocodingDto>())!;
        address.City.ShouldBe("Яремче");
        address.Region.ShouldBe("Івано-Франківська область");
    }

    [Fact]
    public async Task Reverse_ShouldReturn404_WhenNominatimCannotGeocodeThePoint()
    {
        using var host = await CreateHostAsync();
        Nominatim.RespondWithJson("""{ "error": "Unable to geocode" }""");

        var response = await host.GetAsync($"{ReverseUrl}?lat=42&lng=-37");

        await response.ShouldHaveStatusAsync(HttpStatusCode.NotFound);
        (await response.ReadProblemTitleAsync()).ShouldBe(GeocodingErrors.NotFound.Code);
    }

    [Fact]
    public async Task Reverse_ShouldServeRepeatedRequestFromCache_WhenCoordinatesAreTheSame()
    {
        using var host = await CreateHostAsync();
        await host.GetAsync($"{ReverseUrl}?lat=50.4501&lng=30.5234");

        var second = await host.GetAsync($"{ReverseUrl}?lat=50.4501&lng=30.5234");

        await second.ShouldHaveStatusAsync(HttpStatusCode.OK);
        Nominatim.Requests.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("lat=91&lng=0")]
    [InlineData("lat=0&lng=-181")]
    [InlineData("lat=50.45")]
    [InlineData("")]
    public async Task Reverse_ShouldReturn400AndNotCallNominatim_WhenCoordinatesAreMissingOrOutOfRange(string query)
    {
        using var host = await CreateHostAsync();

        var response = await host.GetAsync($"{ReverseUrl}?{query}");

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        Nominatim.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Search_ShouldReturnCandidates_WhenUserIsAnonymous()
    {
        var response = await Client.GetAsync(Search("Хрещатик, Київ"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<List<GeocodingCandidateDto>>())!.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Search_ShouldReturnCandidates_WhenUserIsClient()
    {
        using var client = await CreateUserClientAsync("client@test.com", Roles.Client);

        var response = await client.GetAsync(Search("Хрещатик, Київ"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Search_ShouldReturn429WithRetryAfter_WhenClientIpExceedsPerIpLimit()
    {
     
        for (var i = 0; i < 10; i++)
        {
            var allowed = await Client.GetAsync(Search("Хрещатик, Київ"));
            await allowed.ShouldHaveStatusAsync(HttpStatusCode.OK);
        }

        var rejected = await Client.GetAsync(Search("Хрещатик, Київ"));

        await rejected.ShouldHaveStatusAsync(HttpStatusCode.TooManyRequests);
        (await rejected.ReadProblemTitleAsync()).ShouldBe(RateLimitingExtensions.RejectedErrorCode);
        rejected.Headers.RetryAfter.ShouldNotBeNull();
        Nominatim.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Search_ShouldStillServeOtherIps_WhenOneClientIpIsRateLimited()
    {
        await ExhaustSearchLimitAsync(Client);

        using var otherClient = CreateClient(NewIp());
        var response = await otherClient.GetAsync(Search("Хрещатик, Київ"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Search_ShouldLimitSignedInUserPerAccount_WhenRequestsComeFromDifferentIps()
    {
        var auth = await RegisterAsync("client@test.com");
        await ExhaustSearchLimitAsync(AuthorizedClientFrom(auth.AccessToken, NewIp()));

        using var sameUserOtherIp = AuthorizedClientFrom(auth.AccessToken, NewIp());
        var response = await sameUserOtherIp.GetAsync(Search("Хрещатик, Київ"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Search_ShouldNotLimitSignedInUser_WhenAnonymousTrafficFromSameIpIsLimited()
    {
        await ExhaustSearchLimitAsync(Client);

        using var user = await CreateUserClientAsync("client@test.com", Roles.Client);
        var response = await user.GetAsync(Search("Хрещатик, Київ"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Search_ShouldFallBackToIpLimit_WhenAccessTokenIsInvalid()
    {
        await ExhaustSearchLimitAsync(Client);

        using var invalidToken = CreateAuthorizedClient("not-a-jwt");
        var response = await invalidToken.GetAsync(Search("Хрещатик, Київ"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Reverse_ShouldReturn401_WhenUserIsAnonymous()
    {
        var response = await Client.GetAsync($"{ReverseUrl}?lat=50&lng=30");

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reverse_ShouldReturn403AndNotCallNominatim_WhenUserIsNotHost()
    {
        using var client = await CreateUserClientAsync("client@test.com", Roles.Client);

        var response = await client.GetAsync($"{ReverseUrl}?lat=50&lng=30");

        await response.ShouldHaveStatusAsync(HttpStatusCode.Forbidden);
        Nominatim.Requests.ShouldBeEmpty();
    }
}
