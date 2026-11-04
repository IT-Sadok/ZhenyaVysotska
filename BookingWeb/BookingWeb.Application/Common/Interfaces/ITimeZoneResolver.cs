using BookingWeb.Domain.ValueObjects;

namespace BookingWeb.Application.Common.Interfaces;

public interface ITimeZoneResolver
{
    string Resolve(GeoPoint point);
}
