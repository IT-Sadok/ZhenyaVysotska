using System.Net;
using System.Net.Http.Json;
using BookingWeb.Application.Auth.Responses;
using BookingWeb.Application.Bookings;
using BookingWeb.Application.Listings;
using BookingWeb.Application.Models;
using BookingWeb.Domain;
using BookingWeb.Domain.Models;
using BookingWeb.Infrastructure.Persistence;
using BookingWeb.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BookingWeb.IntegrationTests.Api;

public sealed class BookingEndpointsTests : ApiTestBase
{
    public BookingEndpointsTests(ApiFactory factory) : base(factory)
    {
        
    }

    private static readonly DateOnly CheckIn = Today.AddDays(10);
    private static readonly DateOnly CheckOut = Today.AddDays(13);

    private async Task<ListingDto> CreateHostListingAsync(decimal pricePerNight = 1000m, int maxGuests = 4)
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        return await CreateListingAsync(host, pricePerNight, maxGuests);
    }

    private static async Task<BookingDto> ReadBookingAsync(HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<BookingDto>())!;
    }

    [Fact]
    public async Task CreateBooking_ShouldReturnCreatedWithTotalPrice_WhenDatesAreFree()
    {
        var listing = await CreateHostListingAsync(pricePerNight: 1250m);
        using var guest = await CreateUserClientAsync("guest@test.com", Roles.Client);

        var response = await BookAsync(guest, listing.Id, CheckIn, CheckOut);

        await response.ShouldHaveStatusAsync(HttpStatusCode.Created);
        var booking = await ReadBookingAsync(response);
        response.Headers.Location!.ToString().ShouldBe($"/api/bookings/{booking.Id}");
        booking.Status.ShouldBe("Confirmed");
        booking.Nights.ShouldBe(3);
        booking.TotalPrice.ShouldBe(3750m);
    }

    [Fact]
    public async Task CreateBooking_ShouldReturnConflict_WhenDatesOverlap()
    {
        var listing = await CreateHostListingAsync();
        using var first = await CreateUserClientAsync("first@test.com", Roles.Client);
        using var second = await CreateUserClientAsync("second@test.com", Roles.Client);
        (await BookAsync(first, listing.Id, CheckIn, CheckOut)).EnsureSuccessStatusCode();

        var response = await BookAsync(second, listing.Id, CheckIn.AddDays(2), CheckOut.AddDays(2));

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
        (await response.ReadProblemTitleAsync()).ShouldBe("Booking.DatesUnavailable");
    }

    [Fact]
    public async Task CreateBooking_ShouldSucceed_WhenCheckInEqualsPreviousCheckOut()
    {
        var listing = await CreateHostListingAsync();
        using var first = await CreateUserClientAsync("first@test.com", Roles.Client);
        using var second = await CreateUserClientAsync("second@test.com", Roles.Client);
        (await BookAsync(first, listing.Id, CheckIn, CheckOut)).EnsureSuccessStatusCode();

        var response = await BookAsync(second, listing.Id, CheckOut, CheckOut.AddDays(2));

        await response.ShouldHaveStatusAsync(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateBooking_ShouldReturnForbidden_WhenUserIsOnlyHost()
    {
        var listing = await CreateHostListingAsync();
        using var otherHost = await CreateUserClientAsync("other-host@test.com", Roles.Host);

        var response = await BookAsync(otherHost, listing.Id, CheckIn, CheckOut);

        await response.ShouldHaveStatusAsync(HttpStatusCode.Forbidden);
    }
    
    [Fact]
    public async Task CreateBooking_ShouldReturnBadRequest_WhenHostBooksOwnListing()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var listing = await CreateListingAsync(host);
        var roleResponse = await host.PutAsync("/api/auth/me/roles/client", content: null);
        await roleResponse.ShouldHaveStatusAsync(HttpStatusCode.OK);
        var token = (await roleResponse.Content.ReadFromJsonAsync<AccessTokenResponse>())!;
        using var hostAsClient = CreateAuthorizedClient(token.AccessToken);

        var response = await BookAsync(hostAsClient, listing.Id, CheckIn, CheckOut);

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        (await response.ReadProblemTitleAsync()).ShouldBe("Booking.OwnListing");
    }

    [Fact]
    public async Task CreateBooking_ShouldReturnValidationProblem_WhenCheckInIsInThePast()
    {
        var listing = await CreateHostListingAsync();
        using var guest = await CreateUserClientAsync("guest@test.com", Roles.Client);

        var response = await BookAsync(guest, listing.Id, Today.AddDays(-1), Today.AddDays(2));

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        (await response.ReadValidationErrorFieldsAsync())
            .ShouldContain(field => field.Equals("CheckIn", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateBooking_ShouldReturnBadRequest_WhenGuestsExceedMaxGuests()
    {
        var listing = await CreateHostListingAsync(maxGuests: 2);
        using var guest = await CreateUserClientAsync("guest@test.com", Roles.Client);

        var response = await BookAsync(guest, listing.Id, CheckIn, CheckOut, guests: 3);

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        (await response.ReadProblemTitleAsync()).ShouldBe("Booking.TooManyGuests");
    }

    [Fact]
    public async Task CreateBooking_ShouldReturnNotFound_WhenListingIsDeleted()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var listing = await CreateListingAsync(host);
        using var guest = await CreateUserClientAsync("guest@test.com", Roles.Client);
        (await host.DeleteAsync($"/api/listings/{listing.Id}")).EnsureSuccessStatusCode();

        var response = await BookAsync(guest, listing.Id, CheckIn, CheckOut);

        await response.ShouldHaveStatusAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBooking_ShouldReturnNotFound_WhenBookingBelongsToOtherGuest()
    {
        var listing = await CreateHostListingAsync();
        using var owner = await CreateUserClientAsync("owner@test.com", Roles.Client);
        using var stranger = await CreateUserClientAsync("stranger@test.com", Roles.Client);
        var booking = await ReadBookingAsync(await BookAsync(owner, listing.Id, CheckIn, CheckOut));

        var response = await stranger.GetAsync($"/api/bookings/{booking.Id}");

        await response.ShouldHaveStatusAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelBooking_ShouldReturnNotFoundAndKeepBooking_WhenBookingBelongsToOtherGuest()
    {
        var listing = await CreateHostListingAsync();
        using var owner = await CreateUserClientAsync("owner@test.com", Roles.Client);
        using var stranger = await CreateUserClientAsync("stranger@test.com", Roles.Client);
        var booking = await ReadBookingAsync(await BookAsync(owner, listing.Id, CheckIn, CheckOut));

        var response = await stranger.PostAsync($"/api/bookings/{booking.Id}/cancel", content: null);

        await response.ShouldHaveStatusAsync(HttpStatusCode.NotFound);
        var stored = await owner.GetFromJsonAsync<BookingDto>($"/api/bookings/{booking.Id}");
        stored!.Status.ShouldBe("Confirmed");
    }

    [Fact]
    public async Task GetMyBookings_ShouldReturnOnlyOwnBookings_WhenSeveralGuestsHaveBookings()
    {
        var listing = await CreateHostListingAsync();
        using var first = await CreateUserClientAsync("first@test.com", Roles.Client);
        using var second = await CreateUserClientAsync("second@test.com", Roles.Client);
        var own = await ReadBookingAsync(await BookAsync(first, listing.Id, CheckIn, CheckOut));
        (await BookAsync(second, listing.Id, CheckOut, CheckOut.AddDays(2))).EnsureSuccessStatusCode();

        var page = await first.GetFromJsonAsync<PagedResult<BookingDto>>("/api/bookings");

        page!.TotalCount.ShouldBe(1);
        page.Items.Single().Id.ShouldBe(own.Id);
    }

    [Fact]
    public async Task CancelBooking_ShouldFreeDates_WhenGuestCancels()
    {
        var listing = await CreateHostListingAsync();
        using var first = await CreateUserClientAsync("first@test.com", Roles.Client);
        using var second = await CreateUserClientAsync("second@test.com", Roles.Client);
        var booking = await ReadBookingAsync(await BookAsync(first, listing.Id, CheckIn, CheckOut));

        var cancelResponse = await first.PostAsync($"/api/bookings/{booking.Id}/cancel", content: null);

        await cancelResponse.ShouldHaveStatusAsync(HttpStatusCode.OK);
        (await ReadBookingAsync(cancelResponse)).Status.ShouldBe("Cancelled");
        await (await BookAsync(second, listing.Id, CheckIn, CheckOut)).ShouldHaveStatusAsync(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CancelBooking_ShouldReturnConflict_WhenBookingIsAlreadyCancelled()
    {
        var listing = await CreateHostListingAsync();
        using var guest = await CreateUserClientAsync("guest@test.com", Roles.Client);
        var booking = await ReadBookingAsync(await BookAsync(guest, listing.Id, CheckIn, CheckOut));
        (await guest.PostAsync($"/api/bookings/{booking.Id}/cancel", content: null)).EnsureSuccessStatusCode();

        var response = await guest.PostAsync($"/api/bookings/{booking.Id}/cancel", content: null);

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
    }
    
    [Fact]
    public async Task CreateBooking_ShouldAcceptExactlyOne_WhenSameDatesAreRequestedConcurrently()
    {
        const int guestCount = 10;
        var listing = await CreateHostListingAsync();

        var guests = new List<HttpClient>();
        for (var i = 0; i < guestCount; i++)
        {
            guests.Add(await CreateUserClientAsync($"guest{i}@test.com", Roles.Client));
        }

        var responses = await Task.WhenAll(
            guests.Select(guest => BookAsync(guest, listing.Id, CheckIn, CheckOut)));

        responses.Count(response => response.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.Conflict).ShouldBe(guestCount - 1);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var confirmed = await db.Bookings.CountAsync(
            booking => booking.ListingId == listing.Id && booking.Status == BookingStatus.Confirmed);
        confirmed.ShouldBe(1);

        guests.ForEach(guest => guest.Dispose());
    }
}
