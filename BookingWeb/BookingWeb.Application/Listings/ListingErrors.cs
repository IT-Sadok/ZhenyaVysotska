using BookingWeb.Application.Results;

namespace BookingWeb.Application.Listings;

public static class ListingErrors
{
    public static readonly Error NotFound =
        new("Listing.NotFound", "Listing is not found.", ErrorType.NotFound);

    public static readonly Error HasUpcomingBookings =
        new("Listing.HasUpcomingBookings", "Listing has upcoming bookings and can't be deleted.", ErrorType.Conflict);
    
    public static readonly Error InvalidBoundingBox =
        new("Listing.InvalidBoundingBox", "Bounding box is invalid.");

    public static readonly Error LocationRequired =
        new("Listing.LocationRequired", "Latitude and longitude are required.");

    public static readonly Error InvalidCountryCode =
        new("Listing.InvalidCountryCode", "Country code must be an ISO 3166-1 alpha-2 code.");
}
