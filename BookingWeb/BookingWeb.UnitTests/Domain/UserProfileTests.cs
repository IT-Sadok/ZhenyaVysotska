using BookingWeb.Domain.Models;
using Shouldly;

namespace BookingWeb.UnitTests.Domain;

public sealed class UserProfileTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_ShouldSetNamesAndLeaveOptionalFieldsEmpty_WhenArgumentsAreValid()
    {
        var profile = UserProfile.Create(UserId, "Ivan", "Petrenko");

        profile.UserId.ShouldBe(UserId);
        profile.FirstName.ShouldBe("Ivan");
        profile.LastName.ShouldBe("Petrenko");
        profile.Bio.ShouldBeNull();
        profile.AvatarUrl.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldTrimNames_WhenNamesHaveSurroundingSpaces()
    {
        var profile = UserProfile.Create(UserId, "  Ivan  ", "  Petrenko  ");

        profile.FirstName.ShouldBe("Ivan");
        profile.LastName.ShouldBe("Petrenko");
    }

    [Fact]
    public void Create_ShouldThrow_WhenUserIdIsEmpty()
    {
        Should.Throw<ArgumentException>(() => UserProfile.Create(Guid.Empty, "Ivan", "Petrenko"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenFirstNameIsBlank(string firstName)
    {
        Should.Throw<ArgumentException>(() => UserProfile.Create(UserId, firstName, "Petrenko"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenLastNameIsBlank(string lastName)
    {
        Should.Throw<ArgumentException>(() => UserProfile.Create(UserId, "Ivan", lastName));
    }

    [Fact]
    public void UpdateName_ShouldReplaceAndTrimNames_WhenNamesAreValid()
    {
        var profile = UserProfile.Create(UserId, "Ivan", "Petrenko");

        profile.UpdateName("  Oksana  ", "  Koval  ");

        profile.FirstName.ShouldBe("Oksana");
        profile.LastName.ShouldBe("Koval");
    }

    [Fact]
    public void UpdateName_ShouldKeepOldNames_WhenNewFirstNameIsBlank()
    {
        var profile = UserProfile.Create(UserId, "Ivan", "Petrenko");

        Should.Throw<ArgumentException>(() => profile.UpdateName("", "Koval"));
        
        profile.FirstName.ShouldBe("Ivan");
        profile.LastName.ShouldBe("Petrenko");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateBio_ShouldSetNull_WhenBioIsBlank(string? bio)
    {
        var profile = UserProfile.Create(UserId, "Ivan", "Petrenko");
        profile.UpdateBio("Old bio");

        profile.UpdateBio(bio);

        profile.Bio.ShouldBeNull();
    }

    [Fact]
    public void UpdateBio_ShouldTrimBio_WhenBioHasSurroundingSpaces()
    {
        var profile = UserProfile.Create(UserId, "Ivan", "Petrenko");

        profile.UpdateBio("  Loves travelling  ");

        profile.Bio.ShouldBe("Loves travelling");
    }

    [Fact]
    public void UpdateAvatar_ShouldSetNull_WhenUrlIsBlank()
    {
        var profile = UserProfile.Create(UserId, "Ivan", "Petrenko");
        profile.UpdateAvatar("https://example.com/a.png");

        profile.UpdateAvatar("   ");

        profile.AvatarUrl.ShouldBeNull();
    }
}
