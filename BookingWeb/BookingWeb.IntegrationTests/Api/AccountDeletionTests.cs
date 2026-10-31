using System.Net;
using System.Net.Http.Json;
using BookingWeb.Application.Accounts;
using BookingWeb.Application.Auth;
using BookingWeb.Application.Auth.Requests;
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

public sealed class AccountDeletionTests : ApiTestBase
{
    public AccountDeletionTests(ApiFactory factory) : base(factory)
    {
        
    }
    
    private static readonly DateOnly CheckIn = Today.AddDays(10);
    private static readonly DateOnly CheckOut = Today.AddDays(13);

    private static Task<HttpResponseMessage> DeleteAccountAsync(HttpClient client, string password = DefaultPassword)
    {
        return client.PostAsJsonAsync("/api/auth/me/deletion", new DeleteAccountRequest(password));
    }
    
    private async Task<Guid> GetUserIdAsync(HttpClient client)
    {
        var me = await client.GetFromJsonAsync<UserDto>("/api/auth/me");
        return me!.Id;
    }

    [Fact]
    public async Task DeleteAccount_ShouldRejectLoginAndRefresh_WhenAccountIsDeleted()
    {
        var auth = await RegisterAsync("ivan@test.com");
        using var client = CreateAuthorizedClient(auth.AccessToken);

        var response = await DeleteAccountAsync(client);

        await response.ShouldHaveStatusAsync(HttpStatusCode.NoContent);

        var login = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest("ivan@test.com", DefaultPassword));
        await login.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);

        var refresh = await Client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(auth.RefreshToken));
        await refresh.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAccount_ShouldRejectStillValidAccessToken_WhenAccountIsDeleted()
    {
        var auth = await RegisterAsync("ivan@test.com", Roles.Host);
        using var client = CreateAuthorizedClient(auth.AccessToken);
        (await DeleteAccountAsync(client)).EnsureSuccessStatusCode();

        await (await client.GetAsync("/api/auth/me")).ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
        await (await client.GetAsync("/api/listings/mine")).ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_ShouldCreateNewAccount_WhenEmailBelongedToDeletedAccount()
    {
        using var oldClient = await CreateUserClientAsync("ivan@test.com", Roles.Client);
        var oldId = await GetUserIdAsync(oldClient);
        (await DeleteAccountAsync(oldClient)).EnsureSuccessStatusCode();

        var auth = await RegisterAsync("ivan@test.com", Roles.Client);

        using var newClient = CreateAuthorizedClient(auth.AccessToken);
        (await GetUserIdAsync(newClient)).ShouldNotBe(oldId);
    }

    [Fact]
    public async Task DeleteAccount_ShouldAnonymizeUserAndKeepPastBookings_WhenGuestHasHistory()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var listing = await CreateListingAsync(host);
        using var guest = await CreateUserClientAsync("guest@test.com", Roles.Client);
        var guestId = await GetUserIdAsync(guest);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Bookings.Add(Booking.Create(
                listing.Id, guestId, Today.AddDays(-10), Today.AddDays(-7), 2, 1000m, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }

        (await DeleteAccountAsync(guest)).EnsureSuccessStatusCode();

        using var verifyScope = Factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await verifyDb.Users.SingleAsync(u => u.Id == guestId);
        user.DeletedAtUtc.ShouldNotBeNull();
        user.Email.ShouldBe($"deleted-{guestId:N}@deleted.invalid");
        user.PasswordHash.ShouldBeNull();
        (await verifyDb.Bookings.CountAsync(b => b.GuestId == guestId)).ShouldBe(1);
        (await verifyDb.UserProfiles.AnyAsync(p => p.UserId == guestId)).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAccount_ShouldDeactivateHostListings_WhenHostHasNoUpcomingBookings()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        await CreateListingAsync(host);
        await CreateListingAsync(host);

        (await DeleteAccountAsync(host)).EnsureSuccessStatusCode();

        var page = await Client.GetFromJsonAsync<PagedResult<ListingDto>>("/api/listings");
        page!.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetUsers_ShouldNotListDeletedAccounts_WhenAdminRequestsUsers()
    {
        using var client = await CreateUserClientAsync("ivan@test.com", Roles.Client);
        (await DeleteAccountAsync(client)).EnsureSuccessStatusCode();
        var admin = await LoginAsAdminAsync();
        using var adminClient = CreateAuthorizedClient(admin.AccessToken);

        var page = await adminClient.GetFromJsonAsync<PagedResult<UserDto>>("/api/admin/users");

        page!.Items.Select(user => user.Email).ShouldBe(new[] { ApiFactory.AdminEmail });
    }
    
    [Fact]
    public async Task DeleteAccount_ShouldReturnBadRequestAndKeepAccount_WhenPasswordIsWrong()
    {
        using var client = await CreateUserClientAsync("ivan@test.com", Roles.Client);

        var response = await DeleteAccountAsync(client, "WrongPassword1!");

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        (await response.ReadProblemTitleAsync()).ShouldBe("Account.InvalidPassword");
        await (await client.GetAsync("/api/auth/me")).ShouldHaveStatusAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteAccount_ShouldReturnForbidden_WhenUserIsAdmin()
    {
        var admin = await LoginAsAdminAsync();
        using var adminClient = CreateAuthorizedClient(admin.AccessToken);

        var response = await DeleteAccountAsync(adminClient, ApiFactory.AdminPassword);

        await response.ShouldHaveStatusAsync(HttpStatusCode.Forbidden);
        (await response.ReadProblemTitleAsync()).ShouldBe("Account.AdminCannotBeDeleted");
    }

    [Fact]
    public async Task DeleteAccount_ShouldReturnConflict_WhenGuestHasUpcomingBooking()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var listing = await CreateListingAsync(host);
        using var guest = await CreateUserClientAsync("guest@test.com", Roles.Client);
        (await BookAsync(guest, listing.Id, CheckIn, CheckOut)).EnsureSuccessStatusCode();

        var response = await DeleteAccountAsync(guest);

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
        (await response.ReadProblemTitleAsync()).ShouldBe("Account.HasUpcomingBookingsAsGuest");
    }

    [Fact]
    public async Task DeleteAccount_ShouldReturnConflictAndKeepListingsActive_WhenHostListingHasUpcomingBooking()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var listing = await CreateListingAsync(host);
        using var guest = await CreateUserClientAsync("guest@test.com", Roles.Client);
        (await BookAsync(guest, listing.Id, CheckIn, CheckOut)).EnsureSuccessStatusCode();

        var response = await DeleteAccountAsync(host);

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
        (await response.ReadProblemTitleAsync()).ShouldBe("Account.HasUpcomingBookingsAsHost");
        await (await Client.GetAsync($"/api/listings/{listing.Id}")).ShouldHaveStatusAsync(HttpStatusCode.OK);
    }
    
    [Fact]
    public async Task DeleteAccount_ShouldSucceed_WhenHostCancelsUpcomingBookingsFirst()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var listing = await CreateListingAsync(host);
        using var guest = await CreateUserClientAsync("guest@test.com", Roles.Client);
        var bookingResponse = await BookAsync(guest, listing.Id, CheckIn, CheckOut);
        var booking = (await bookingResponse.Content.ReadFromJsonAsync<BookingWeb.Application.Bookings.BookingDto>())!;
        (await DeleteAccountAsync(host)).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        (await host.PostAsync($"/api/listings/{listing.Id}/bookings/{booking.Id}/cancel", content: null))
            .EnsureSuccessStatusCode();

        await (await DeleteAccountAsync(host)).ShouldHaveStatusAsync(HttpStatusCode.NoContent);
    }
}