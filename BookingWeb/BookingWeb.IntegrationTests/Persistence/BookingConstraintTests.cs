using BookingWeb.Application.Bookings;
using BookingWeb.Application.Common.Interfaces;
using BookingWeb.Domain;
using BookingWeb.Domain.Models;
using BookingWeb.Infrastructure.Persistence;
using BookingWeb.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;

namespace BookingWeb.IntegrationTests.Persistence;

public sealed class BookingConstraintTests : ApiTestBase
{
    public BookingConstraintTests(ApiFactory factory) : base(factory)
    {
        
    }

    private static readonly DateOnly CheckIn = Today.AddDays(10);
    private static readonly DateOnly CheckOut = Today.AddDays(13);

    private async Task<(Guid ListingId, Guid GuestId)> ArrangeListingAndGuestAsync()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var listing = await CreateListingAsync(host);
        await RegisterAsync("guest@test.com", Roles.Client);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var guestId = await db.Users
            .Where(user => user.Email == "guest@test.com")
            .Select(user => user.Id)
            .SingleAsync();

        return (listing.Id, guestId);
    }

    private static Booking CreateBooking(Guid listingId, Guid guestId, DateOnly checkIn, DateOnly checkOut)
    {
        return Booking.Create(listingId, guestId, checkIn, checkOut, 2, 1000m, DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task SaveChanges_ShouldThrowExclusionViolation_WhenConfirmedBookingsOverlap()
    {
        var (listingId, guestId) = await ArrangeListingAndGuestAsync();
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Bookings.Add(CreateBooking(listingId, guestId, CheckIn, CheckOut));
        await db.SaveChangesAsync();

        db.Bookings.Add(CreateBooking(listingId, guestId, CheckIn.AddDays(1), CheckOut.AddDays(1)));
        var exception = await Should.ThrowAsync<DbUpdateException>(() => db.SaveChangesAsync());

        var postgres = exception.InnerException.ShouldBeOfType<PostgresException>();
        postgres.SqlState.ShouldBe(PostgresErrorCodes.ExclusionViolation);
    }

    [Fact]
    public async Task SaveChanges_ShouldAllowOverlap_WhenEarlierBookingIsCancelled()
    {
        var (listingId, guestId) = await ArrangeListingAndGuestAsync();
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cancelled = CreateBooking(listingId, guestId, CheckIn, CheckOut);
        cancelled.Cancel(DateTimeOffset.UtcNow, BookingCanceller.Guest);
        db.Bookings.Add(cancelled);
        await db.SaveChangesAsync();

        db.Bookings.Add(CreateBooking(listingId, guestId, CheckIn, CheckOut));

        await Should.NotThrowAsync(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task UnitOfWorkSave_ShouldThrowBookingOverlapException_WhenBookingsOverlap()
    {
        var (listingId, guestId) = await ArrangeListingAndGuestAsync();
        using var scope = Factory.Services.CreateScope();
        var bookings = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        bookings.Add(CreateBooking(listingId, guestId, CheckIn, CheckOut));
        await unitOfWork.SaveChangesAsync();

        bookings.Add(CreateBooking(listingId, guestId, CheckIn, CheckOut));

        await Should.ThrowAsync<BookingOverlapException>(() => unitOfWork.SaveChangesAsync());
    }
}
