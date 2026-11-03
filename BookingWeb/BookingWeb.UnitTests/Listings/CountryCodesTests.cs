using BookingWeb.Application.Common.Constants;
using Shouldly;

namespace BookingWeb.UnitTests.Listings;

public sealed class CountryCodesTests
{
    [Fact]
    public void All_ShouldContain249OfficiallyAssignedCodes_WhenLoaded()
    {
        CountryCodes.All.Count.ShouldBe(249);
    }

    [Fact]
    public void All_ShouldContainOnlyTwoUppercaseLatinLetters_WhenLoaded()
    {
        CountryCodes.All.ShouldAllBe(code => code.Length == 2 && code.All(c => c >= 'A' && c <= 'Z'));
    }

    [Theory]
    [InlineData("UA")]
    [InlineData("PL")]
    [InlineData("US")]
    [InlineData("GB")]
    [InlineData("ua")]
    [InlineData(" pl ")]
    public void IsValid_ShouldReturnTrue_WhenCodeIsAssigned(string code)
    {
        CountryCodes.IsValid(code).ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("XX")]
    [InlineData("XK")]  
    [InlineData("UK")]  
    [InlineData("EU")]
    [InlineData("UKR")]
    public void IsValid_ShouldReturnFalse_WhenCodeIsNotAssigned(string? code)
    {
        CountryCodes.IsValid(code).ShouldBeFalse();
    }
}
