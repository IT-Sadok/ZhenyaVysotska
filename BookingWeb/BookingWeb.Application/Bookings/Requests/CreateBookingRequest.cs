namespace BookingWeb.Application.Bookings.Requests;

public sealed record CreateBookingRequest( 
    Guid ListingId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Guests);