using Ardalis.GuardClauses;

namespace BookingWeb.Domain.ValueObjects;

public sealed record GeoPoint
{
    public const double MinLatitude = -90;
    public const double MaxLatitude = 90;
    public const double MinLongitude = -180;
    public const double MaxLongitude = 180;

    public double Latitude { get; }
    public double Longitude { get; }

    public GeoPoint(double latitude, double longitude)
    {
        Guard.Against.Expression(value => !double.IsFinite(value), latitude, "Latitude must be a finite number.");
        Guard.Against.Expression(value => !double.IsFinite(value), longitude, "Longitude must be a finite number.");
        Guard.Against.OutOfRange(latitude, nameof(latitude), MinLatitude, MaxLatitude);
        Guard.Against.OutOfRange(longitude, nameof(longitude), MinLongitude, MaxLongitude);

        Latitude = latitude;
        Longitude = longitude;
    }
}
