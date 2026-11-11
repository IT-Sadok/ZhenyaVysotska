using BookingWeb.Application.Listings;
using BookingWeb.Application.Listings.Requests;
using BookingWeb.Application.Listings.Validators;
using FluentValidation.TestHelper;

namespace BookingWeb.UnitTests.Listings;

public sealed class ListingRequestValidatorTests
{
    private readonly ListingRequestValidator _validator = new();

    private static ListingRequest CreateValidRequest()
    {
        return new ListingRequest(
            "Loft Studio", "Cozy", "Kyiv", "Kyiv", "Khreshchatyk 1", 50.4501, 30.5234, 1500.50m, 2);
    }

    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        _validator.TestValidate(CreateValidRequest()).ShouldNotHaveAnyValidationErrors();
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Validate_ShouldFail_WhenPriceIsNotPositive(int price)
    {
        var request = CreateValidRequest() with { PricePerNight = price };

        _validator.TestValidate(request)
            .ShouldHaveValidationErrorFor(x => x.PricePerNight);
    }

    [Fact]
    public void Validate_ShouldFail_WhenPriceHasMoreThanTwoDecimals()
    {
        var request = CreateValidRequest() with { PricePerNight = 10.555m };

        _validator.TestValidate(request)
            .ShouldHaveValidationErrorFor(x => x.PricePerNight);
    }

    [Fact]
    public void Validate_ShouldFail_WhenTitleIsEmpty()
    {
        var request = CreateValidRequest() with { Title = "" };

        _validator.TestValidate(request)
            .ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_ShouldFail_WhenMaxGuestsIsZero()
    {
        var request = CreateValidRequest() with { MaxGuests = 0 };

        _validator.TestValidate(request)
            .ShouldHaveValidationErrorFor(x => x.MaxGuests);
    }
    
    [Fact]
    public void Validate_ShouldPass_WhenPriceHasExact18DigitsPrecision()
    {
        var request = CreateValidRequest() with { PricePerNight = 7_000_000_000_000_000.44m };
        
        _validator.TestValidate(request)
            .ShouldNotHaveValidationErrorFor(x => x.PricePerNight);
    }
    
    [Fact]
    public void Validate_ShouldFail_WhenPriceExceeds18DigitsPrecision()
    {
        var request = CreateValidRequest() with { PricePerNight = 70_000_000_000_000_000.44m };

        _validator.TestValidate(request)
            .ShouldHaveValidationErrorFor(x => x.PricePerNight)
            .WithErrorCode("ScalePrecisionValidator");
    }

    [Fact]
    public void Validate_ShouldPass_WhenRegionIsNull()
    {
        var request = CreateValidRequest() with { Region = null };

        _validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenRegionIsTooLong()
    {
        var request = CreateValidRequest() with { Region = new string('a', 101) };

        _validator.TestValidate(request)
            .ShouldHaveValidationErrorFor(x => x.Region);
    }

    [Fact]
    public void Validate_ShouldFail_WhenLatitudeIsMissing()
    {
        var request = CreateValidRequest() with { Latitude = null };

        _validator.TestValidate(request)
            .ShouldHaveValidationErrorFor(x => x.Latitude)
            .WithErrorCode(ListingErrors.LocationRequired.Code);
    }

    [Fact]
    public void Validate_ShouldFail_WhenLongitudeIsMissing()
    {
        var request = CreateValidRequest() with { Longitude = null };

        _validator.TestValidate(request)
            .ShouldHaveValidationErrorFor(x => x.Longitude)
            .WithErrorCode(ListingErrors.LocationRequired.Code);
    }

    [Theory]
    [InlineData(-90.000001)]
    [InlineData(90.000001)]
    public void Validate_ShouldFail_WhenLatitudeIsOutOfRange(double latitude)
    {
        var request = CreateValidRequest() with { Latitude = latitude };

        _validator.TestValidate(request)
            .ShouldHaveValidationErrorFor(x => x.Latitude);
    }

    [Theory]
    [InlineData(-180.000001)]
    [InlineData(180.000001)]
    public void Validate_ShouldFail_WhenLongitudeIsOutOfRange(double longitude)
    {
        var request = CreateValidRequest() with { Longitude = longitude };

        _validator.TestValidate(request)
            .ShouldHaveValidationErrorFor(x => x.Longitude);
    }

    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    public void Validate_ShouldPass_WhenCoordinatesAreOnBoundaries(double latitude, double longitude)
    {
        var request = CreateValidRequest() with { Latitude = latitude, Longitude = longitude };

        _validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Validate_ShouldFail_WhenLatitudeIsNotFinite(double latitude)
    {
        var request = CreateValidRequest() with { Latitude = latitude };

        _validator.TestValidate(request)
            .ShouldHaveValidationErrorFor(x => x.Latitude);
    }
}
