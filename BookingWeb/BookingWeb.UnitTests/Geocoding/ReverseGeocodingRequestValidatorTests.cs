using BookingWeb.Application.Geocoding.Requests;
using BookingWeb.Application.Geocoding.Validators;
using FluentValidation.TestHelper;

namespace BookingWeb.UnitTests.Geocoding;

public sealed class ReverseGeocodingRequestValidatorTests
{
    private readonly ReverseGeocodingRequestValidator _validator = new();

    [Theory]
    [InlineData(50.4501, 30.5234)]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    public void Validate_ShouldPass_WhenCoordinatesAreInRange(double lat, double lng)
    {
        _validator.TestValidate(new ReverseGeocodingRequest(lat, lng)).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(-90.0001)]
    [InlineData(90.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Validate_ShouldFail_WhenLatitudeIsOutOfRange(double lat)
    {
        _validator.TestValidate(new ReverseGeocodingRequest(lat, 30)).ShouldHaveValidationErrorFor(x => x.Lat);
    }

    [Theory]
    [InlineData(-180.0001)]
    [InlineData(180.0001)]
    [InlineData(double.NaN)]
    public void Validate_ShouldFail_WhenLongitudeIsOutOfRange(double lng)
    {
        _validator.TestValidate(new ReverseGeocodingRequest(50, lng)).ShouldHaveValidationErrorFor(x => x.Lng);
    }

    [Fact]
    public void Validate_ShouldFail_WhenCoordinatesAreMissing()
    {
        var result = _validator.TestValidate(new ReverseGeocodingRequest(null, null));

        result.ShouldHaveValidationErrorFor(x => x.Lat);
        result.ShouldHaveValidationErrorFor(x => x.Lng);
    }
}
