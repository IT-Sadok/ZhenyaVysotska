using BookingWeb.Application.Listings.Requests;
using BookingWeb.Application.Listings.Validators;
using BookingWeb.UnitTests.TestDoubles;
using FluentValidation.TestHelper;

namespace BookingWeb.UnitTests.Listings;

public sealed class ListingFilterDtoValidatorTests
{
    private static readonly DateOnly Today = new(2026, 3, 1);

    private readonly ListingFilterDtoValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenFilterIsEmpty()
    {
        _validator.TestValidate(new ListingFilterDto(null, null))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenPageSizeIsAbove100()
    {
        var filter = new ListingFilterDto(null, null, PageSize: 101);

        _validator.TestValidate(filter).ShouldHaveValidationErrorFor(x => x.PageSize);
    }
}
