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
            "Loft Studio", "Cozy", "Kyiv", "Khreshchatyk 1", 1500.50m, 2);
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
}