using BookingWeb.Application.Common.Interfaces;
using BookingWeb.Domain.ValueObjects;

namespace BookingWeb.UnitTests.TestDoubles;

public sealed class FakeTimeZoneResolver : ITimeZoneResolver
{
    public string TimeZoneId { get; set; } = "Europe/Kyiv";

    public List<GeoPoint> Calls { get; } = [];

    public string Resolve(GeoPoint point)
    {
        Calls.Add(point);
        return TimeZoneId;
    }
}
