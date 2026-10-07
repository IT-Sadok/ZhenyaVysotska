using BookingWeb.Application.Profiles;
using BookingWeb.Application.Profiles.Requests;
using FluentValidation.TestHelper;

namespace BookingWeb.UnitTests.Profiles;

public sealed class UpdateProfileRequestValidatorTests
{
    private readonly UpdateProfileRequestValidator _validator = new();

    private static UpdateProfileRequest CreateValidRequest()
    {
        return new UpdateProfileRequest("Ivan", "Petrenko", "Loves travelling", "https://example.com/a.png");
    }

    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        var result = _validator.TestValidate(CreateValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldPass_WhenBioAndAvatarAreNull()
    {
        var result = _validator.TestValidate(CreateValidRequest() with { Bio = null, AvatarUrl = null });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenFirstNameIsEmpty()
    {
        var result = _validator.TestValidate(CreateValidRequest() with { FirstName = "" });

        result.ShouldHaveValidationErrorFor(request => request.FirstName);
    }

    [Fact]
    public void Validate_ShouldFail_WhenLastNameIsEmpty()
    {
        var result = _validator.TestValidate(CreateValidRequest() with { LastName = "" });

        result.ShouldHaveValidationErrorFor(request => request.LastName);
    }

    [Fact]
    public void Validate_ShouldFail_WhenFirstNameIsLongerThan128Characters()
    {
        var result = _validator.TestValidate(CreateValidRequest() with { FirstName = new string('a', 129) });

        result.ShouldHaveValidationErrorFor(request => request.FirstName);
    }

    [Fact]
    public void Validate_ShouldFail_WhenBioIsLongerThan1000Characters()
    {
        var result = _validator.TestValidate(CreateValidRequest() with { Bio = new string('a', 1001) });

        result.ShouldHaveValidationErrorFor(request => request.Bio);
    }

    [Fact]
    public void Validate_ShouldFail_WhenAvatarUrlIsLongerThan500Characters()
    {
        var result = _validator.TestValidate(CreateValidRequest() with { AvatarUrl = new string('a', 501) });

        result.ShouldHaveValidationErrorFor(request => request.AvatarUrl);
    }
}
