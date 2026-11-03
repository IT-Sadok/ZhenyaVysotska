using BookingWeb.Application.Listings;
using BookingWeb.Domain.ValueObjects;

namespace BookingWeb.UnitTests.TestDoubles;

public sealed class FakeLocationObfuscator : ILocationObfuscator
{
    public const double LatitudeOffset = 0.003;

    public List<GeoPoint> Calls { get; } = [];

    public GeoPoint Obfuscate(GeoPoint exact)
    {
        Calls.Add(exact);
        return new GeoPoint(exact.Latitude + LatitudeOffset, exact.Longitude);
    }
}
