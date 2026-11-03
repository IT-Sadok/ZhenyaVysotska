using BookingWeb.Application.Listings;
using BookingWeb.Domain.ValueObjects;
using BookingWeb.UnitTests.TestDoubles;
using Shouldly;

namespace BookingWeb.UnitTests.Listings;

public sealed class LocationObfuscatorTests
{
    private const int Runs = 1000;
    
    private const double ToleranceMeters = 0.01;

    private readonly LocationObfuscator _sut = new();

    public static TheoryData<double, double> Origins => new()
    {
        { 50.4501, 30.5234 },     // Kyiv
        { 0, 0 },                 // equator 
        { -33.8688, 151.2093 },   // southern hemisphere
        { 64.1466, -21.9426 },    // high latitude: a metre of longitude is "wider" in degrees
        { 0, 179.9999 },          // ~11 m from the 180th meridian
        { 0, -179.9999 },
        { 89.9999, 0 },           // ~11 m from the North Pole
        { -89.9999, 0 },
        { 90, 0 },                // exactly at the pole
    };

    [Theory]
    [MemberData(nameof(Origins))]
    public void Obfuscate_ShouldMovePoint200To500Meters_WhenRunManyTimes(double latitude, double longitude)
    {
        var exact = new GeoPoint(latitude, longitude);

        for (var i = 0; i < Runs; i++)
        {
            var obfuscated = _sut.Obfuscate(exact);

            var distance = GeoDistance.Meters(exact, obfuscated);
            distance.ShouldBeGreaterThanOrEqualTo(LocationObfuscator.MinDistanceMeters - ToleranceMeters);
            distance.ShouldBeLessThanOrEqualTo(LocationObfuscator.MaxDistanceMeters + ToleranceMeters);
        }
    }

    [Theory]
    [MemberData(nameof(Origins))]
    public void Obfuscate_ShouldReturnValidPointWithinRanges_WhenOriginIsTypicalOrNearEdges(double latitude, double longitude)
    {
        var exact = new GeoPoint(latitude, longitude);

        for (var i = 0; i < Runs; i++)
        {
            var obfuscated = _sut.Obfuscate(exact);

            obfuscated.Latitude.ShouldBeInRange(GeoPoint.MinLatitude, GeoPoint.MaxLatitude);
            obfuscated.Longitude.ShouldBeInRange(GeoPoint.MinLongitude, GeoPoint.MaxLongitude);
            obfuscated.ShouldNotBe(exact);
        }
    }

    [Fact]
    public void Obfuscate_ShouldCrossAntimeridianWithNormalizedLongitude_WhenPointIsNextToIt()
    {
        var exact = new GeoPoint(0, 179.9999);

        var longitudes = Enumerable.Range(0, Runs).Select(_ => _sut.Obfuscate(exact).Longitude).ToList();

        longitudes.ShouldContain(lng => lng < 0);
        longitudes.ShouldAllBe(lng => lng >= -180 && lng < 180);
    }

    [Fact]
    public void Obfuscate_ShouldSpreadInAllDirections_WhenRunManyTimes()
    {
        var exact = new GeoPoint(50.4501, 30.5234);

        var quadrants = Enumerable.Range(0, Runs)
            .Select(_ => _sut.Obfuscate(exact))
            .Select(p => (North: p.Latitude > exact.Latitude, East: p.Longitude > exact.Longitude))
            .GroupBy(q => q)
            .ToDictionary(g => g.Key, g => g.Count());

        quadrants.Count.ShouldBe(4);
        quadrants.Values.ShouldAllBe(count => count > 150);
    }

    [Fact]
    public void Obfuscate_ShouldReturnDifferentPoints_WhenCalledTwiceForSameOrigin()
    {
        var exact = new GeoPoint(50.4501, 30.5234);

        _sut.Obfuscate(exact).ShouldNotBe(_sut.Obfuscate(exact));
    }
}
