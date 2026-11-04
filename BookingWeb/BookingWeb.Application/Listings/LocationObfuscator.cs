using System.Security.Cryptography;
using BookingWeb.Domain.ValueObjects;

namespace BookingWeb.Application.Listings;

public sealed class LocationObfuscator : ILocationObfuscator
{
    public const double MinDistanceMeters = 200;
    public const double MaxDistanceMeters = 500;

    public const double EarthRadiusMeters = 6_371_008.8;

    public GeoPoint Obfuscate(GeoPoint exact)
    {
        var bearing = NextDouble() * 2 * Math.PI;
        
        var minSquared = MinDistanceMeters * MinDistanceMeters;
        var maxSquared = MaxDistanceMeters * MaxDistanceMeters;
        var distance = Math.Sqrt(NextDouble() * (maxSquared - minSquared) + minSquared);

        return Destination(exact, bearing, distance);
    }
    
    private static GeoPoint Destination(GeoPoint start, double bearing, double distanceMeters)
    {
        var angularDistance = distanceMeters / EarthRadiusMeters;
        var lat1 = DegreesToRadians(start.Latitude);
        var lng1 = DegreesToRadians(start.Longitude);

        var lat2 = Math.Asin(
            Math.Sin(lat1) * Math.Cos(angularDistance) +
            Math.Cos(lat1) * Math.Sin(angularDistance) * Math.Cos(bearing));

        var lng2 = lng1 + Math.Atan2(
            Math.Sin(bearing) * Math.Sin(angularDistance) * Math.Cos(lat1),
            Math.Cos(angularDistance) - Math.Sin(lat1) * Math.Sin(lat2));

        var latitude = Math.Clamp(RadiansToDegrees(lat2), GeoPoint.MinLatitude, GeoPoint.MaxLatitude);
        return new GeoPoint(latitude, NormalizeLongitude(RadiansToDegrees(lng2)));
    }

    private static double NormalizeLongitude(double longitude)
    {
        var normalized = (longitude + 180) % 360;
        if (normalized < 0)
        {
            normalized += 360;
        }

        return normalized - 180;
    }
    
    private static double NextDouble()
    {
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        RandomNumberGenerator.Fill(bytes);
        
        var value = BitConverter.ToUInt64(bytes) >> 11;
        return value * (1.0 / (1UL << 53));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;

    private static double RadiansToDegrees(double radians) => radians * 180 / Math.PI;
}
