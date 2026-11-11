using BookingWeb.Application.Results;

namespace BookingWeb.Application.Geocoding;

public static class GeocodingErrors
{
    public static readonly Error Unavailable =
        new("Geocoding.Unavailable", "Geocoding service is unavailable. Try again later.", ErrorType.Unavailable);

    public static readonly Error Busy =
        new("Geocoding.Busy", "Too many geocoding requests right now. Try again in a few seconds.", ErrorType.Unavailable);

    public static readonly Error NotFound =
        new("Geocoding.NotFound", "No address found for these coordinates.", ErrorType.NotFound);
}
