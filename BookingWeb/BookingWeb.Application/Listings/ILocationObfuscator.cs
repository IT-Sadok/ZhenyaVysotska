using BookingWeb.Domain.ValueObjects;

namespace BookingWeb.Application.Listings;

public interface ILocationObfuscator
{
    GeoPoint Obfuscate(GeoPoint exact);
}
