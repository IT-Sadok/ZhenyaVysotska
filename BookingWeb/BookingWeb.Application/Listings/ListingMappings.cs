using BookingWeb.Domain.Models;

namespace BookingWeb.Application.Listings;

internal static class ListingMappings
{
    public static ListingDto ToDto(this Listing listing)
    {
        return new ListingDto(
            listing.Id,
            listing.HostId,
            listing.Title,
            listing.Description,
            listing.City,
            listing.Address,
            listing.PricePerNight,
            listing.MaxGuests,
            listing.IsActive);
    }
}