namespace BookingWeb.Domain.Models;

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
        Guard.ThrowIfEmpty(listingId);
        Guard.ThrowIfEmpty(guestId);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(checkOut, checkIn);
        ArgumentOutOfRangeException.ThrowIfLessThan(guests, 1);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pricePerNight);
        
        var nights = checkOut.DayNumber - checkIn.DayNumber;

        return new Booking
        {
            Id = Guid.NewGuid(),
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

    public bool CanBeCancelledAt(DateTimeOffset nowUtc)
    {
        var today = DateOnly.FromDateTime(nowUtc.UtcDateTime);
        return Status == BookingStatus.Confirmed && today < CheckIn;
    }

    public void Cancel(DateTimeOffset nowUtc, BookingCanceller canceller)
    {
        if (!CanBeCancelledAt(nowUtc))
        {
            throw new InvalidOperationException(
                "Only a confirmed booking can be cancelled, and only before the check-in day.");
        }

        Status = BookingStatus.Cancelled;
        CancelledAtUtc = nowUtc;
        CancelledBy = canceller;
    }
}

public enum BookingStatus
{
    Confirmed,
    Cancelled
}

public enum BookingCanceller
{
    Guest,
    Host
}