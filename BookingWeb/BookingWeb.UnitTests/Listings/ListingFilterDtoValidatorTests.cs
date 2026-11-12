using BookingWeb.Application.Listings;
using BookingWeb.Application.Listings.Requests;
using BookingWeb.Application.Listings.Validators;
using BookingWeb.UnitTests.TestDoubles;
using FluentValidation.TestHelper;

namespace BookingWeb.UnitTests.Listings;

public sealed class ListingFilterDtoValidatorTests
{
    private const string BoundingBox = "BoundingBox";
    private static readonly string InvalidBox = ListingErrors.InvalidBoundingBox.Code;
    private static readonly DateOnly Today = new(2026, 3, 1);

    private readonly ListingFilterDtoValidator _validator = 
        new(new FixedTimeProvider(new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero)));

    private static ListingFilterDto WithBox(double minLat = 50.3, double minLng = 30.3, double maxLat = 50.6, double maxLng = 30.8) =>
        new(MinLat: minLat, MinLng: minLng, MaxLat: maxLat, MaxLng: maxLng);

    [Fact]
    public void Validate_ShouldPass_WhenFilterIsEmpty()
    {
        _validator.TestValidate(new ListingFilterDto()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllFourBoundingBoxValuesAreGiven()
    {
        _validator.TestValidate(WithBox()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenPageSizeIsAbove100()
    {
        var filter = new ListingFilterDto(PageSize: 101);

        _validator.TestValidate(filter).ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Validate_ShouldFail_WhenGuestsIsZero()
    {
        _validator.TestValidate(new ListingFilterDto(Guests: 0)).ShouldHaveValidationErrorFor(x => x.Guests);
    }

    [Fact]
    public void Validate_ShouldFail_WhenMaxPriceIsBelowMinPrice()
    {
        var filter = new ListingFilterDto(MinPrice: 2000m, MaxPrice: 1000m);

        _validator.TestValidate(filter).ShouldHaveValidationErrorFor(x => x.MaxPrice);
    }

    [Fact]
    public void Validate_ShouldFail_WhenMaxPriceIsNegativeAndMinPriceIsMissing()
    {
        var filter = new ListingFilterDto(MaxPrice: -1m);

        _validator.TestValidate(filter).ShouldHaveValidationErrorFor(x => x.MaxPrice);
    }

    public static TheoryData<ListingFilterDto> PartialBoxes => new()
    {
        new ListingFilterDto(MinLat: 50.3),
        new ListingFilterDto(MinLat: 50.3, MinLng: 30.3),
        new ListingFilterDto(MinLat: 50.3, MinLng: 30.3, MaxLat: 50.6),
        new ListingFilterDto(MinLng: 30.3, MaxLat: 50.6, MaxLng: 30.8),
        new ListingFilterDto(MaxLng: 30.8),
    };

    [Theory]
    [MemberData(nameof(PartialBoxes))]
    public void Validate_ShouldFailWithInvalidBoundingBox_WhenOnlySomeBoxValuesAreGiven(ListingFilterDto filter)
    {
        _validator.TestValidate(filter)
            .ShouldHaveValidationErrorFor(BoundingBox)
            .WithErrorCode(InvalidBox);
    }

    [Fact]
    public void Validate_ShouldFailWithInvalidBoundingBox_WhenMinLatIsGreaterThanMaxLat()
    {
        _validator.TestValidate(WithBox(minLat: 50.6, maxLat: 50.3))
            .ShouldHaveValidationErrorFor(BoundingBox)
            .WithErrorCode(InvalidBox);
    }

    [Fact]
    public void Validate_ShouldFailWithInvalidBoundingBox_WhenBoxCrossesAntimeridian()
    {
        // 170°E → 170°W would need minLng > maxLng, which is not supported.
        _validator.TestValidate(WithBox(minLng: 170, maxLng: -170))
            .ShouldHaveValidationErrorFor(BoundingBox)
            .WithErrorCode(InvalidBox);
    }

    [Fact]
    public void Validate_ShouldFailWithInvalidBoundingBox_WhenBoxIsTooLarge()
    {
        _validator.TestValidate(WithBox(minLat: 40, maxLat: 50.000001))
            .ShouldHaveValidationErrorFor(BoundingBox)
            .WithErrorCode(InvalidBox);
    }

    [Fact]
    public void Validate_ShouldPass_WhenBoxIsExactlyMaxSpan()
    {
        _validator.TestValidate(WithBox(minLat: 40, maxLat: 50, minLng: 20, maxLng: 30))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFailWithInvalidBoundingBox_WhenLatitudeIsOutOfRange()
    {
        _validator.TestValidate(WithBox(minLat: -91, maxLat: -85))
            .ShouldHaveValidationErrorFor(x => x.MinLat)
            .WithErrorCode(InvalidBox);
    }

    [Fact]
    public void Validate_ShouldFailWithInvalidBoundingBox_WhenLongitudeIsOutOfRange()
    {
        _validator.TestValidate(WithBox(minLng: 175, maxLng: 181))
            .ShouldHaveValidationErrorFor(x => x.MaxLng)
            .WithErrorCode(InvalidBox);
    }

    [Fact]
    public void Validate_ShouldPass_WhenStayStartsToday()
    {
        _validator.TestValidate(new ListingFilterDto(CheckIn: Today, CheckOut: Today.AddDays(2)))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenOnlyCheckInIsGiven()
    {
        _validator.TestValidate(new ListingFilterDto(CheckIn: Today))
            .ShouldHaveValidationErrorFor(x => x.CheckOut);
    }

    [Fact]
    public void Validate_ShouldFail_WhenOnlyCheckOutIsGiven()
    {
        _validator.TestValidate(new ListingFilterDto(CheckOut: Today.AddDays(2)))
            .ShouldHaveValidationErrorFor(x => x.CheckIn);
    }
    
    [Fact]
    public void Validate_ShouldFail_WhenCheckInIsYesterdayInKyiv()
    {
        _validator.TestValidate(new ListingFilterDto(CheckIn: Today.AddDays(-1), CheckOut: Today.AddDays(2)))
            .ShouldHaveValidationErrorFor(x => x.CheckIn);
    }

    [Fact]
    public void Validate_ShouldFail_WhenCheckInIsTodayInUtcButYesterdayInKyiv()
    {
        // 22:30 UTC on Feb 28 is already 00:30 on Mar 1 in Kyiv (UTC+2 in winter).
        var validator = new ListingFilterDtoValidator(new FixedTimeProvider(new DateTimeOffset(2026, 2, 28, 22, 30, 0, TimeSpan.Zero)));

        validator.TestValidate(new ListingFilterDto(CheckIn: new DateOnly(2026, 2, 28), CheckOut: Today.AddDays(2)))
            .ShouldHaveValidationErrorFor(x => x.CheckIn);
    }

    [Fact]
    public void Validate_ShouldFail_WhenCheckOutIsNotAfterCheckIn()
    {
        _validator.TestValidate(new ListingFilterDto(CheckIn: Today.AddDays(3), CheckOut: Today.AddDays(3)))
            .ShouldHaveValidationErrorFor(x => x.CheckOut);
    }
}
