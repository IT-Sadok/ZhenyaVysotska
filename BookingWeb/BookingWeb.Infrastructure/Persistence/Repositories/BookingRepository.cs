using BookingWeb.Application.Common.Interfaces;
using BookingWeb.Application.Models;
using BookingWeb.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingWeb.Infrastructure.Persistence.Repositories;

public sealed class BookingRepository : IBookingRepository
{
    private readonly ApplicationDbContext _db;
    public BookingRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public void Add(Booking booking)
    {
        _db.Bookings.Add(booking);
    }

    public Task<PagedResult<Booking>> GetForListingAsync(Guid listingId, PageRequest request, CancellationToken ct = default)
    {
        return _db.Bookings
            .AsNoTracking()
            .Where(b=> b.ListingId == listingId)
            .OrderByDescending(b => b.CheckIn)
            .ThenBy(b=>b.Id)
            .ToPagedResultAsync(request.Page, request.PageSize, ct);
    }
    
    public Task<Booking?> GetByIdForGuestAsync(Guid bookingId, Guid guestId, CancellationToken ct = default)
    {
        return _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.GuestId == guestId, ct);
    }

    public Task<PagedResult<Booking>> GetForGuestAsync(Guid guestId, PageRequest request, CancellationToken ct = default)
    {
        return _db.Bookings
            .AsNoTracking()
            .Where(b=> b.GuestId == guestId)
            .OrderByDescending(b => b.CheckIn)
            .ThenBy(b=>b.Id)
            .ToPagedResultAsync(request.Page, request.PageSize, ct);
    }
    
    public Task<Booking?> GetByIdForHostAsync(Guid bookingId, Guid hostId, CancellationToken ct = default)
    {
        return _db.Bookings.FirstOrDefaultAsync(
            b => b.Id == bookingId 
                 && _db.Listings.Any(l => l.Id == b.ListingId && l.HostId == hostId), ct);
    }
    
    public Task<bool> HasOverlapAsync(
        Guid listingId, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default)
    {
        return _db.Bookings
            .Where(b => b.ListingId == listingId)
            .ThatBlock(checkIn, checkOut)
            .AnyAsync(ct);
    }

    public Task<bool> HasUpcomingForListingAsync(Guid listingId, DateOnly today, CancellationToken ct = default)
    {
        return _db.Bookings.AnyAsync(
            b=> b.ListingId == listingId
                && b.Status == BookingStatus.Confirmed
                && b.CheckOut > today, ct);
    }
    
    public Task<bool> HasUpcomingForGuestAsync(Guid guestId, DateOnly today, CancellationToken ct = default)
    {
        return _db.Bookings.AnyAsync(
            b => b.GuestId == guestId
                 && b.Status == BookingStatus.Confirmed
                 && b.CheckOut > today, ct);
    }

    public Task<bool> HasUpcomingForHostAsync(Guid hostId, DateOnly today, CancellationToken ct = default)
    {
        return _db.Bookings.AnyAsync(
            b => b.Status == BookingStatus.Confirmed
                        && b.CheckOut > today
                        && _db.Listings.Any(l => l.Id == b.ListingId && l.HostId == hostId), ct);
    }
}