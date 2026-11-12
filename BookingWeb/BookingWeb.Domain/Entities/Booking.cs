using System.Linq.Expressions;
using Ardalis.GuardClauses;

namespace BookingWeb.Domain.Entities;

public sealed class Booking
{
    public Guid Id { get; private set; }
    public Guid ListingId { get; private set; }
    public Guid GuestId { get; private set; }
    public DateOnly CheckIn { get; private set; }
    public DateOnly CheckOut { get; private set; }
    public int Guests { get; private set; }
    public decimal TotalPrice { get; private set; }
    public BookingStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public BookingCanceller? CancelledBy { get; private set; }
    
    public int Nights => CheckOut.DayNumber - CheckIn.DayNumber;
    
    private Booking() { }
    
    public static Booking Create(
        Guid listingId,
        Guid guestId,
        DateOnly checkIn,
        DateOnly checkOut,
        int guests,
        decimal pricePerNight,
        DateTimeOffset createdAtUtc)
    {
        Guard.Against.Default(listingId);
        Guard.Against.Default(guestId);
        Guard.Against.Expression(
            checkOutDate => checkOutDate <= checkIn,
            checkOut,
            "Check-out must be after check-in.");
        Guard.Against.NegativeOrZero(guests);
        Guard.Against.NegativeOrZero(pricePerNight);
        
        var nights = checkOut.DayNumber - checkIn.DayNumber;

        return new Booking
        {
            Id = Guid.CreateVersion7(createdAtUtc),
            ListingId = listingId,
            GuestId = guestId,
            CheckIn = checkIn,
            CheckOut = checkOut,
            Guests = guests,
            TotalPrice = pricePerNight * nights,
            Status = BookingStatus.Confirmed,
            CreatedAtUtc = createdAtUtc
        };
    }
    
    public static Expression<Func<Booking, bool>> Blocks(DateOnly checkIn, DateOnly checkOut) =>
        booking => booking.Status == BookingStatus.Confirmed
                   && booking.CheckIn < checkOut
                   && checkIn < booking.CheckOut;

    public static Expression<Func<Booking, bool>> IsUpcoming(DateOnly today) =>
        booking => booking.Status == BookingStatus.Confirmed
                   && booking.CheckOut > today;
    
    public bool CanBeCancelledOn(DateOnly today)
    {
        return Status == BookingStatus.Confirmed && today < CheckIn;
    }

    public void Cancel(DateTimeOffset nowUtc, DateOnly today, BookingCanceller canceller)
    {
        if (!CanBeCancelledOn(today))
        {
            throw new InvalidOperationException(
                "Only a confirmed booking can be cancelled, and only before the check-in day.");
        }

        Status = BookingStatus.Cancelled;
        CancelledAtUtc = nowUtc;
        CancelledBy = canceller;
    }
}
