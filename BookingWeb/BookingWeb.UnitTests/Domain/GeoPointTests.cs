using BookingWeb.Domain.ValueObjects;
using Shouldly;

namespace BookingWeb.UnitTests.Domain;

public sealed class GeoPointTests
{
    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    [InlineData(-90, 180)]
    [InlineData(90, -180)]
    [InlineData(0, 0)]
    [InlineData(48.2922, 24.5546)]
    public void Ctor_ShouldCreatePoint_WhenCoordinatesAreWithinRange(double latitude, double longitude)
    {
        var point = new GeoPoint(latitude, longitude);

        point.Latitude.ShouldBe(latitude);
        point.Longitude.ShouldBe(longitude);
    }

    [Theory]
    [InlineData(-90.000001)]
    [InlineData(90.000001)]
    [InlineData(-1000)]
    [InlineData(1000)]
    public void Ctor_ShouldThrowOutOfRange_WhenLatitudeIsOutOfRange(double latitude)
    {
        var exception = Should.Throw<ArgumentOutOfRangeException>(() => new GeoPoint(latitude, 0));

        exception.ParamName.ShouldBe("latitude");
    }

    [Theory]
    [InlineData(-180.000001)]
    [InlineData(180.000001)]
    [InlineData(-1000)]
    [InlineData(1000)]
    public void Ctor_ShouldThrowOutOfRange_WhenLongitudeIsOutOfRange(double longitude)
    {
        var exception = Should.Throw<ArgumentOutOfRangeException>(() => new GeoPoint(0, longitude));

        exception.ParamName.ShouldBe("longitude");
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Ctor_ShouldThrow_WhenLatitudeIsNotFinite(double latitude)
    {
        Should.Throw<ArgumentException>(() => new GeoPoint(latitude, 0));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Ctor_ShouldThrow_WhenLongitudeIsNotFinite(double longitude)
    {
        Should.Throw<ArgumentException>(() => new GeoPoint(0, longitude));
    }

    [Fact]
    public void Equals_ShouldBeTrue_WhenCoordinatesAreEqual()
    {
        var first = new GeoPoint(50.4501, 30.5234);
        var second = new GeoPoint(50.4501, 30.5234);

        first.ShouldBe(second);
        (first == second).ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldBeFalse_WhenCoordinatesDiffer()
    {
        var first = new GeoPoint(50.4501, 30.5234);
        var second = new GeoPoint(50.4501, 30.5235);

        (first == second).ShouldBeFalse();
    }
}
