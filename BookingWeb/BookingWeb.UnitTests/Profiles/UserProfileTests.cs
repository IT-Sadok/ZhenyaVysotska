using BookingWeb.Domain.Models;
using Shouldly;

namespace BookingWeb.UnitTests.Profiles;

public class UserProfileTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_valid_profile()
    {
        var profile = UserProfile.Create(UserId, "Ivan", "Petrenko");

        profile.UserId.ShouldBe(UserId);
        profile.FirstName.ShouldBe("Ivan");
        profile.LastName.ShouldBe("Petrenko");
        profile.Bio.ShouldBeNull();
        profile.AvatarUrl.ShouldBeNull();
    }

    [Fact]
    public void Create_trims_names()
    {
        var profile = UserProfile.Create(UserId, "  Ivan  ", "  Petrenko  ");

        profile.FirstName.ShouldBe("Ivan");
        profile.LastName.ShouldBe("Petrenko");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_empty_first_name_throws(string firstName)
    {
        Should.Throw<ArgumentException>(() => UserProfile.Create(UserId, firstName, "Petrenko"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_empty_last_name_throws(string lastName)
    {
        Should.Throw<ArgumentException>(() => UserProfile.Create(UserId, "Ivan", lastName));
    }

    [Fact]
    public void Create_empty_user_id_throws()
    {
        Should.Throw<ArgumentException>(() => UserProfile.Create(Guid.Empty, "Ivan", "Petrenko"));
    }

    [Fact]
    public void UpdateName_changes_and_trims()
    {
        var profile = UserProfile.Create(UserId, "Ivan", "Petrenko");

        profile.UpdateName("  Oksana  ", "  Koval  ");

        profile.FirstName.ShouldBe("Oksana");
        profile.LastName.ShouldBe("Koval");
    }

    [Fact]
    public void UpdateName_empty_throws()
    {
        var profile = UserProfile.Create(UserId, "Ivan", "Petrenko");
        Should.Throw<ArgumentException>(() => profile.UpdateName("", "Petrenko"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateBio_blank_becomes_null(string? bio)
    {
        var profile = UserProfile.Create(UserId, "Ivan", "Petrenko");

        profile.UpdateBio(bio);

        profile.Bio.ShouldBeNull();
    }

    [Fact]
    public void UpdateBio_trims_value()
    {
        var profile = UserProfile.Create(UserId, "Ivan", "Petrenko");

        profile.UpdateBio("  Люблю подорожі  ");

        profile.Bio.ShouldBe("Люблю подорожі");
    }

    [Fact]
    public void UpdateAvatar_blank_becomes_null()
    {
        var profile = UserProfile.Create(UserId, "Ivan", "Petrenko");

        profile.UpdateAvatar("   ");

        profile.AvatarUrl.ShouldBeNull();
    }
}
