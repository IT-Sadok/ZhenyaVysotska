using BookingWeb.Application.Common.Interfaces;
using BookingWeb.Domain.ValueObjects;
using GeoTimeZone;

namespace BookingWeb.Infrastructure.TimeZones;

public sealed class GeoTimeZoneResolver : ITimeZoneResolver
{
    public string Resolve(GeoPoint point)
    {
        return TimeZoneLookup.GetTimeZone(point.Latitude, point.Longitude).Result;
    }
}
