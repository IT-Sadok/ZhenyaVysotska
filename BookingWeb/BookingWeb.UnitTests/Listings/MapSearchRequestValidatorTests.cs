using BookingWeb.Application.Listings;
using BookingWeb.Application.Listings.Requests;
using BookingWeb.Application.Listings.Validators;
using BookingWeb.UnitTests.TestDoubles;
using FluentValidation.TestHelper;
using Shouldly;

namespace BookingWeb.UnitTests.Listings;

public sealed class MapSearchRequestValidatorTests
{
    private static readonly string InvalidBox = ListingErrors.InvalidBoundingBox.Code;

    private static readonly DateOnly Today = new(2026, 3, 1);

    private readonly MapSearchRequestValidator _validator =
        new(new FixedTimeProvider(new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero)));

    private static MapSearchRequest Valid() => new(50.3, 30.3, 50.6, 30.8);

    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Ctor_ShouldDefaultLimitTo200_WhenLimitIsNotGiven()
    {
        Valid().Limit.ShouldBe(200);
    }

    [Fact]
    public void Validate_ShouldFailWithInvalidBoundingBox_WhenBoxValueIsMissing()
    {
        var request = Valid() with { MaxLat = null };

        _validator.TestValidate(request)
            .ShouldHaveValidationErrorFor(x => x.MaxLat)
            .WithErrorCode(InvalidBox);
    }

    [Fact]
    public void Validate_ShouldFailWithInvalidBoundingBox_WhenMinLngIsGreaterThanMaxLng()
    {
        _validator.TestValidate(Valid() with { MinLng = 30.8, MaxLng = 30.3 })
            .ShouldHaveValidationErrorFor("BoundingBox")
            .WithErrorCode(InvalidBox);
    }

    [Fact]
    public void Validate_ShouldFailWithInvalidBoundingBox_WhenMinEqualsMax()
    {
        _validator.TestValidate(Valid() with { MinLat = 50.3, MaxLat = 50.3 })
            .ShouldHaveValidationErrorFor("BoundingBox")
            .WithErrorCode(InvalidBox);
    }

    [Fact]
    public void Validate_ShouldFailWithInvalidBoundingBox_WhenBoxIsTooWide()
    {
        _validator.TestValidate(Valid() with { MinLng = 10, MaxLng = 25 })
            .ShouldHaveValidationErrorFor("BoundingBox")
            .WithErrorCode(InvalidBox);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(-90.5)]
    [InlineData(90.5)]
    public void Validate_ShouldFailWithInvalidBoundingBox_WhenLatitudeIsNotValid(double latitude)
    {
        _validator.TestValidate(Valid() with { MinLat = latitude })
            .ShouldHaveValidationErrorFor(x => x.MinLat)
            .WithErrorCode(InvalidBox);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public void Validate_ShouldFail_WhenLimitIsOutOfRange(int limit)
    {
        _validator.TestValidate(Valid() with { Limit = limit }).ShouldHaveValidationErrorFor(x => x.Limit);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(200)]
    public void Validate_ShouldPass_WhenLimitIsOnBoundary(int limit)
    {
        _validator.TestValidate(Valid() with { Limit = limit }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenMaxPriceIsBelowMinPrice()
    {
        _validator.TestValidate(Valid() with { MinPrice = 2000m, MaxPrice = 1000m })
            .ShouldHaveValidationErrorFor(x => x.MaxPrice);
    }

    [Fact]
    public void Validate_ShouldPass_WhenStayIsInTheFuture()
    {
        _validator.TestValidate(Valid() with { CheckIn = Today.AddDays(1), CheckOut = Today.AddDays(4) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenCheckInIsInThePast()
    {
        _validator.TestValidate(Valid() with { CheckIn = Today.AddDays(-1), CheckOut = Today.AddDays(2) })
            .ShouldHaveValidationErrorFor(x => x.CheckIn);
    }
}
