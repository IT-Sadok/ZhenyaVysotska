using System.Net;
using System.Net.Http.Json;
using BookingWeb.Application.Bookings;
using BookingWeb.Application.Models;
using BookingWeb.Domain;
using BookingWeb.IntegrationTests.Infrastructure;
using Shouldly;

namespace BookingWeb.IntegrationTests.Api;

public sealed class HostBookingsTests : ApiTestBase
{
    public HostBookingsTests(ApiFactory factory) : base(factory)
    {
        
    }

    private static readonly DateOnly CheckIn = Today.AddDays(10);
    private static readonly DateOnly CheckOut = Today.AddDays(13);

    private static async Task<BookingDto> BookAndReadAsync(HttpClient guest, Guid listingId)
    {
        var response = await BookAsync(guest, listingId, CheckIn, CheckOut);
        await response.ShouldHaveStatusAsync(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<BookingDto>())!;
    }

    [Fact]
    public async Task GetListingBookings_ShouldReturnBookings_WhenHostOwnsListing()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var listing = await CreateListingAsync(host);
        using var guest = await CreateUserClientAsync("guest@test.com", Roles.Client);
        var booking = await BookAndReadAsync(guest, listing.Id);

        var page = await host.GetFromJsonAsync<PagedResult<BookingDto>>($"/api/listings/{listing.Id}/bookings");

        page!.Items.Select(item => item.Id).ShouldBe(new[] { booking.Id });
    }

    [Fact]
    public async Task GetListingBookings_ShouldReturnNotFound_WhenListingBelongsToOtherHost()
    {
        using var owner = await CreateUserClientAsync("owner@test.com", Roles.Host);
        var listing = await CreateListingAsync(owner);
        using var otherHost = await CreateUserClientAsync("other@test.com", Roles.Host);

        var response = await otherHost.GetAsync($"/api/listings/{listing.Id}/bookings");

        await response.ShouldHaveStatusAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelByHost_ShouldCancelAsHostAndFreeDates_WhenHostOwnsListing()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var listing = await CreateListingAsync(host);
        using var guest = await CreateUserClientAsync("guest@test.com", Roles.Client);
        using var nextGuest = await CreateUserClientAsync("next@test.com", Roles.Client);
        var booking = await BookAndReadAsync(guest, listing.Id);

        var response = await host.PostAsync($"/api/listings/{listing.Id}/bookings/{booking.Id}/cancel", content: null);

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        var cancelled = (await response.Content.ReadFromJsonAsync<BookingDto>())!;
        cancelled.Status.ShouldBe("Cancelled");
        cancelled.CancelledBy.ShouldBe("Host");
        await BookAndReadAsync(nextGuest, listing.Id);
    }

    [Fact]
    public async Task CancelByHost_ShouldReturnNotFoundAndKeepBooking_WhenListingBelongsToOtherHost()
    {
        using var owner = await CreateUserClientAsync("owner@test.com", Roles.Host);
        var listing = await CreateListingAsync(owner);
        using var otherHost = await CreateUserClientAsync("other@test.com", Roles.Host);
        using var guest = await CreateUserClientAsync("guest@test.com", Roles.Client);
        var booking = await BookAndReadAsync(guest, listing.Id);

        var response = await otherHost.PostAsync($"/api/listings/{listing.Id}/bookings/{booking.Id}/cancel", content: null);

        await response.ShouldHaveStatusAsync(HttpStatusCode.NotFound);
        var stored = await guest.GetFromJsonAsync<BookingDto>($"/api/bookings/{booking.Id}");
        stored!.Status.ShouldBe("Confirmed");
    }
}
