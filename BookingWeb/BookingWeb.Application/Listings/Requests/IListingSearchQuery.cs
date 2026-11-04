namespace BookingWeb.Application.Listings.Requests;

public interface IListingSearchQuery
{
    int? Guests { get; }
    decimal? MinPrice { get; }
    decimal? MaxPrice { get; }

    double? MinLat { get; }
    double? MinLng { get; }
    double? MaxLat { get; }
    double? MaxLng { get; }

    DateOnly? CheckIn { get; }
    DateOnly? CheckOut { get; }
}
