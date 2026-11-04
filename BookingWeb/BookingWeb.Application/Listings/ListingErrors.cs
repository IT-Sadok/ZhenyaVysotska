using BookingWeb.Application.Results;

namespace BookingWeb.Application.Listings;

public static class ListingErrors
{
    public static readonly Error NotFound =
        new("Listing.NotFound", "Listing is not found.", ErrorType.NotFound);

    public static readonly Error HasUpcomingBookings =
        new("Listing.HasUpcomingBookings", "Listing has upcoming bookings and can't be deleted.", ErrorType.Conflict);
}
