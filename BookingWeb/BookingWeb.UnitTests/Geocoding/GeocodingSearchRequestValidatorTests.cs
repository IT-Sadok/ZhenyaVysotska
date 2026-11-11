using BookingWeb.Application.Geocoding.Requests;
using BookingWeb.Application.Geocoding.Validators;
using FluentValidation.TestHelper;

namespace BookingWeb.UnitTests.Geocoding;

public sealed class GeocodingSearchRequestValidatorTests
{
    private readonly GeocodingSearchRequestValidator _validator = new();

    [Theory]
    [InlineData("Хрещатик, Київ")]
    [InlineData("abc")]
    [InlineData("  abc  ")]
    public void Validate_ShouldPass_WhenTrimmedQueryHasAllowedLength(string query)
    {
        _validator.TestValidate(new GeocodingSearchRequest(query)).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    [InlineData("   ab   ")]
    public void Validate_ShouldFail_WhenQueryIsMissingOrTooShort(string? query)
    {
        _validator.TestValidate(new GeocodingSearchRequest(query)).ShouldHaveValidationErrorFor(x => x.Q);
    }

    [Fact]
    public void Validate_ShouldFail_WhenQueryIsTooLong()
    {
        var query = new string('a', GeocodingSearchRequestValidator.MaxQueryLength + 1);

        _validator.TestValidate(new GeocodingSearchRequest(query)).ShouldHaveValidationErrorFor(x => x.Q);
    }

    [Fact]
    public void Validate_ShouldPass_WhenQueryHasMaxLength()
    {
        var query = new string('a', GeocodingSearchRequestValidator.MaxQueryLength);

        _validator.TestValidate(new GeocodingSearchRequest(query)).ShouldNotHaveAnyValidationErrors();
    }
}
