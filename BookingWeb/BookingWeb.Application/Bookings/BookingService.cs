using BookingWeb.Application.Bookings.Requests;
using BookingWeb.Application.Common.Interfaces;
using BookingWeb.Application.Models;
using BookingWeb.Application.Results;
using BookingWeb.Domain.Models;

namespace BookingWeb.Application.Bookings;

public sealed class BookingService : IBookingService
{
    private readonly IBookingRepository _bookings;
    private readonly IListingRepository _listings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public BookingService(
        IBookingRepository bookings,
        IListingRepository listings,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _bookings = bookings;
        _listings = listings;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<BookingDto>> CreateAsync(
        Guid guestId, CreateBookingRequest request, CancellationToken ct = default)
    {
        var listing = await _listings.GetByIdAsync(request.ListingId, ct);
        if (listing is null || !listing.IsActive)
        {
            return BookingErrors.ListingNotFound;
        }

        if (listing.HostId == guestId)
        {
            return BookingErrors.OwnListing;
        }

        if (request.Guests > listing.MaxGuests)
        {
            return BookingErrors.TooManyGuests(listing.MaxGuests);
        }

        if (await _bookings.HasOverlapAsync(listing.Id, request.CheckIn, request.CheckOut, ct))
        {
            return BookingErrors.DatesUnavailable;
        }

        var booking = Booking.Create(
            listing.Id,
            guestId,
            request.CheckIn,
            request.CheckOut,
            request.Guests,
            listing.PricePerNight,
            _timeProvider.GetUtcNow());

        _bookings.Add(booking);

        try
        { 
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (BookingOverlapException)
        {
            return BookingErrors.DatesUnavailable;
        }

        return booking.ToDto();
    }

    public async Task<Result<PagedResult<BookingDto>>> GetMineAsync(
        Guid guestId, PageRequest request, CancellationToken ct = default)
    {
        var page = await _bookings.GetForGuestAsync(guestId, request, ct);
        var items = page.Items.Select(booking => booking.ToDto()).ToList();
        
        return new PagedResult<BookingDto>(items, page.TotalCount, page.Page, page.PageSize);
    }

    public async Task<Result<BookingDto>> GetByIdAsync(
        Guid guestId, Guid bookingId, CancellationToken ct = default)
    {
        var booking = await _bookings.GetByIdForGuestAsync(bookingId, guestId, ct);
        if (booking is null)
        {
            return BookingErrors.NotFound;
        }

        return booking.ToDto();
    }
    
    public async Task<Result<PagedResult<BookingDto>>> GetForHostListingAsync(
        Guid hostId, Guid listingId, PageRequest request, CancellationToken ct = default)
    {
        var listing = await _listings.GetByIdForHostAsync(listingId, hostId, ct);
        if (listing is null)
        {
            return BookingErrors.ListingNotFound;
        }

        var page = await _bookings.GetForListingAsync(listing.Id, request, ct);

        var items = page.Items.Select(booking => booking.ToDto()).ToList();
        return new PagedResult<BookingDto>(items, page.TotalCount, page.Page, page.PageSize);
    }

    public async Task<Result<BookingDto>> CancelAsync(
        Guid guestId, Guid bookingId, CancellationToken ct = default)
    {
        var booking = await _bookings.GetByIdForGuestAsync(bookingId, guestId, ct);
        if (booking is null)
        {
            return BookingErrors.NotFound;
        }

        var now = _timeProvider.GetUtcNow();
        if (!booking.CanBeCancelledAt(now))
        {
            return BookingErrors.CannotBeCancelled;
        }

        booking.Cancel(now, BookingCanceller.Guest);
        await _unitOfWork.SaveChangesAsync(ct);

        return booking.ToDto();
    }
    
    public async Task<Result<BookingDto>> CancelByHostAsync(
        Guid hostId, Guid listingId, Guid bookingId, CancellationToken ct = default)
    {
        var booking = await _bookings.GetByIdForHostAsync(bookingId, hostId, ct);
        if (booking is null || booking.ListingId != listingId)
        {
            return BookingErrors.NotFound;
        }

        var now = _timeProvider.GetUtcNow();
        if (!booking.CanBeCancelledAt(now))
        {
            return BookingErrors.CannotBeCancelled;
        }

        booking.Cancel(now, BookingCanceller.Host);
        await _unitOfWork.SaveChangesAsync(ct);

        return booking.ToDto();
    }
}
