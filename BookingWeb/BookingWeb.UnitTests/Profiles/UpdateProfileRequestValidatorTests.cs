using BookingWeb.Application.Profiles;
using BookingWeb.Application.Profiles.Requests;
using FluentValidation.TestHelper;

namespace BookingWeb.UnitTests.Profiles;

public class UpdateProfileRequestValidatorTests
{
    private readonly UpdateProfileRequestValidator _validator = new();

    private static UpdateProfileRequest Valid() =>
        new("Ivan", "Petrenko", "Люблю подорожі", "https://example.com/a.png");

    [Fact]
    public void Valid_request_passes()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_first_name_fails()
    {
        _validator.TestValidate(Valid() with { FirstName = "" })
            .ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Empty_last_name_fails()
    {
        _validator.TestValidate(Valid() with { LastName = "" })
            .ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void Too_long_first_name_fails()
    {
        _validator.TestValidate(Valid() with { FirstName = new string('a', 129) })
            .ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Too_long_bio_fails()
    {
        _validator.TestValidate(Valid() with { Bio = new string('a', 1001) })
            .ShouldHaveValidationErrorFor(x => x.Bio);
    }

    [Fact]
    public void Null_bio_and_avatar_pass()
    {
        _validator.TestValidate(Valid() with { Bio = null, AvatarUrl = null })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Too_long_avatar_url_fails()
    {
        _validator.TestValidate(Valid() with { AvatarUrl = "https://x/" + new string('a', 500) })
            .ShouldHaveValidationErrorFor(x => x.AvatarUrl);
    }

}