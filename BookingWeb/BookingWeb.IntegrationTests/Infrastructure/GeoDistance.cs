using BookingWeb.Application.Listings;

namespace BookingWeb.IntegrationTests.Infrastructure;

public static class GeoDistance
{
    public static double Meters(double lat1Deg, double lng1Deg, double lat2Deg, double lng2Deg)
    {
        var lat1 = lat1Deg * Math.PI / 180;
        var lat2 = lat2Deg * Math.PI / 180;
        var dLat = lat2 - lat1;
        var dLng = (lng2Deg - lng1Deg) * Math.PI / 180;

        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        return 2 * LocationObfuscator.EarthRadiusMeters * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }
}
