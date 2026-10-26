using BookingWeb.Application.Models;
using BookingWeb.Domain.Models;

namespace BookingWeb.Application.Common.Interfaces;

public interface IBookingRepository
{
    void Add (Booking booking);
    
    Task<Booking?> GetByIdForGuestAsync(Guid bookingId, Guid guestId, CancellationToken ct = default);
    
    Task<PagedResult<Booking>> GetForGuestAsync(Guid guestId, PageRequest request, CancellationToken ct = default);
    
    Task<Booking?> GetByIdForHostAsync(Guid bookingId, Guid hostId, CancellationToken ct = default);

    Task<PagedResult<Booking>> GetForListingAsync(Guid listingId, PageRequest request, CancellationToken ct = default);
    
    Task<bool> HasOverlapAsync(Guid listingId, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default);
    
    Task<bool> HasUpcomingForListingAsync(Guid listingId, DateOnly today, CancellationToken ct = default);
    
    Task<bool> HasUpcomingForGuestAsync(Guid guestId, DateOnly today, CancellationToken ct = default);
    
    Task<bool> HasUpcomingForHostAsync(Guid hostId, DateOnly today, CancellationToken ct = default);
}