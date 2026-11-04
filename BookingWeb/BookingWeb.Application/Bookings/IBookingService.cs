using BookingWeb.Application.Bookings.Requests;
using BookingWeb.Application.Models;
using BookingWeb.Application.Results;

namespace BookingWeb.Application.Bookings;

public interface IBookingService
{
    Task<Result<BookingDto>> CreateAsync(Guid guestId, CreateBookingRequest request, CancellationToken ct = default);

    Task<Result<PagedResult<BookingDto>>> GetMineAsync(Guid guestId, PageRequest request, CancellationToken ct = default);

    Task<Result<BookingDto>> GetByIdAsync(Guid guestId, Guid bookingId, CancellationToken ct = default);

    Task<Result<BookingDto>> CancelAsync(Guid guestId, Guid bookingId, CancellationToken ct = default);

    Task<Result<PagedResult<BookingDto>>> GetForHostListingAsync(
        Guid hostId, Guid listingId, PageRequest request, CancellationToken ct = default);

    Task<Result<BookingDto>> CancelByHostAsync(
        Guid hostId, Guid listingId, Guid bookingId, CancellationToken ct = default);
}