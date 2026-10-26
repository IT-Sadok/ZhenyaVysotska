using BookingWeb.Application.Results;

namespace BookingWeb.Application.Bookings;

public static class BookingErrors
{
    public static readonly Error NotFound =
        new("Booking.NotFound", "Booking is not found.", ErrorType.NotFound);

    public static readonly Error ListingNotFound =
        new("Booking.ListingNotFound", "Listing is not found.", ErrorType.NotFound);

    public static readonly Error OwnListing =
        new("Booking.OwnListing", "You can't book your own listing.");

    public static readonly Error DatesUnavailable =
        new("Booking.DatesUnavailable", "The listing is already booked for some of these nights.", ErrorType.Conflict);

    public static readonly Error CannotBeCancelled =
        new("Booking.CannotBeCancelled",
            "Only a confirmed booking can be cancelled, and only before the check-in day.",
            ErrorType.Conflict);

    public static Error TooManyGuests(int maxGuests) =>
        new("Booking.TooManyGuests", $"This listing allows at most {maxGuests} guests.");
}
