using BookingWeb.Application.Bookings.Requests;
using BookingWeb.Application.Common;
using BookingWeb.Application.Common.Interfaces;
using BookingWeb.Application.Models;
using BookingWeb.Application.Results;
using BookingWeb.Domain.Entities;
using Mapster;

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
        
        if (request.CheckIn < _timeProvider.GetPlatformToday())
        {
            return BookingErrors.CheckInInPast;
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
        
        var saveResult = await _unitOfWork.TrySaveChangesAsync(ct);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return booking.Adapt<BookingDto>();
    }

    public async Task<Result<PagedResult<BookingDto>>> GetMineAsync(
        Guid guestId, PageRequest request, CancellationToken ct = default)
    {
        var page = await _bookings.GetForGuestAsync(guestId, request, ct);
        var items = page.Items.Adapt<List<BookingDto>>();
        
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

        return booking.Adapt<BookingDto>();
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

        var items = page.Items.Adapt<List<BookingDto>>();
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

        return await CancelAsync(booking, BookingCanceller.Guest, ct);
    }
    
    public async Task<Result<BookingDto>> CancelByHostAsync(
        Guid hostId, Guid listingId, Guid bookingId, CancellationToken ct = default)
    {
        var booking = await _bookings.GetByIdForHostAsync(bookingId, hostId, ct);
        if (booking is null || booking.ListingId != listingId)
        {
            return BookingErrors.NotFound;
        }

        return await CancelAsync(booking, BookingCanceller.Host, ct);
    }

    private async Task<Result<BookingDto>> CancelAsync(
        Booking booking, BookingCanceller canceller, CancellationToken ct)
    {
        var listing = await _listings.GetByIdAsync(booking.ListingId, ct);
        if (listing is null)
        {
            return BookingErrors.ListingNotFound;
        }

        var today = _timeProvider.GetPlatformToday();
        if (!booking.CanBeCancelledOn(today))
        {
            return BookingErrors.CannotBeCancelled;
        }

        booking.Cancel(_timeProvider.GetUtcNow(), today, canceller);
        await _unitOfWork.SaveChangesAsync(ct);

        return booking.Adapt<BookingDto>();
    }
}
