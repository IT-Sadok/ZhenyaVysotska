using BookingWeb.Domain;
using Shouldly;

namespace BookingWeb.UnitTests.Domain;

public sealed class RolesTests
{
    [Theory]
    [InlineData("host", Roles.Host)]
    [InlineData("HOST", Roles.Host)]
    [InlineData("Client", Roles.Client)]
    [InlineData("admin", Roles.Admin)]
    public void GetExactRoleName_ShouldReturnCanonicalName_WhenRoleExistsInAnyCase(string input, string expected)
    {
        Roles.GetExactRoleName(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Superuser")]
    public void GetExactRoleName_ShouldReturnNull_WhenRoleIsUnknown(string input)
    {
        Roles.GetExactRoleName(input).ShouldBeNull();
    }

    [Fact]
    public void SelfAssignable_ShouldNotContainAdmin()
    {
        Roles.SelfAssignable.ShouldNotContain(Roles.Admin);
    }
}
