using BookingWeb.Domain.Models;

namespace BookingWeb.Application.Bookings;

internal static class BookingMappings
{
    public static BookingDto ToDto(this Booking booking)
    {
        return new BookingDto(
            booking.Id,
            booking.ListingId,
            booking.CheckIn,
            booking.CheckOut,
            booking.Nights,
            booking.Guests,
            booking.TotalPrice,
            booking.Status.ToString(),
            booking.CreatedAtUtc,
            booking.CancelledAtUtc,
            booking.CancelledBy?.ToString());
    }
}
