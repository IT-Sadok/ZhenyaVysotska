using BookingWeb.Application.Listings;
using BookingWeb.Domain.ValueObjects;

namespace BookingWeb.UnitTests.TestDoubles;

public static class GeoDistance
{
    public static double Meters(GeoPoint a, GeoPoint b)
    {
        var lat1 = a.Latitude * Math.PI / 180;
        var lat2 = b.Latitude * Math.PI / 180;
        var dLat = lat2 - lat1;
        var dLng = (b.Longitude - a.Longitude) * Math.PI / 180;

        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        return 2 * LocationObfuscator.EarthRadiusMeters * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }
}
