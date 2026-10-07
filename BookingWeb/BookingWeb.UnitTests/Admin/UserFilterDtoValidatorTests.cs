using BookingWeb.Application.Admin.Validators;
using BookingWeb.Application.Constants;
using BookingWeb.Application.Models;
using BookingWeb.Domain;
using FluentValidation.TestHelper;

namespace BookingWeb.UnitTests.Admin;

public sealed class UserFilterDtoValidatorTests
{
    private readonly UserFilterDtoValidator _validator = new();

    private static UserFilterDto CreateEmptyFilter()
    {
        return new UserFilterDto(Role: null, Email: null, SortBy: null, Page: 1, PageSize: 20);
    }

    [Fact]
    public void Validate_ShouldPass_WhenFilterIsEmpty()
    {
        var result = _validator.TestValidate(CreateEmptyFilter());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldPass_WhenRoleIsBlank(string role)
    {
        var result = _validator.TestValidate(CreateEmptyFilter() with { Role = role });

        result.ShouldNotHaveValidationErrorFor(filter => filter.Role);
    }

    [Theory]
    [InlineData("Client")]
    [InlineData("Host")]
    [InlineData("Admin")]
    public void Validate_ShouldPass_WhenRoleIsKnown(string role)
    {
        var result = _validator.TestValidate(CreateEmptyFilter() with { Role = role });

        result.ShouldNotHaveValidationErrorFor(filter => filter.Role);
    }
    
    [Theory]
    [InlineData("cLient")]
    [InlineData("host")]
    [InlineData("ADmin")]
    [InlineData("hOst")]
    public void Validate_ShouldPass_WhenRoleIsKnownInAnyCase(string role)
    {
        var result = _validator.TestValidate(CreateEmptyFilter() with { Role = role });

        result.ShouldNotHaveValidationErrorFor(filter => filter.Role);
    }

    [Fact]
    public void Validate_ShouldFail_WhenRoleIsUnknown()
    {
        var result = _validator.TestValidate(CreateEmptyFilter() with { Role = "Superuser" });

        result.ShouldHaveValidationErrorFor(filter => filter.Role);
    }

    [Theory]
    [InlineData(SortTokens.EmailAsc)]
    [InlineData(SortTokens.EmailDesc)]
    public void Validate_ShouldPass_WhenSortTokenIsKnown(string sortBy)
    {
        var result = _validator.TestValidate(CreateEmptyFilter() with { SortBy = sortBy });

        result.ShouldNotHaveValidationErrorFor(filter => filter.SortBy);
    }

    [Fact]
    public void Validate_ShouldFail_WhenSortTokenIsUnknown()
    {
        var result = _validator.TestValidate(CreateEmptyFilter() with { SortBy = "email" });

        result.ShouldHaveValidationErrorFor(filter => filter.SortBy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldFail_WhenPageIsLessThanOne(int page)
    {
        var result = _validator.TestValidate(CreateEmptyFilter() with { Page = page });

        result.ShouldHaveValidationErrorFor(filter => filter.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(100_000)] 
    public void Validate_ShouldFail_WhenPageSizeIsOutOfRange(int pageSize)
    {
        var result = _validator.TestValidate(CreateEmptyFilter() with { PageSize = pageSize });

        result.ShouldHaveValidationErrorFor(filter => filter.PageSize);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Validate_ShouldPass_WhenPageSizeIsOnBoundary(int pageSize)
    {
        var result = _validator.TestValidate(CreateEmptyFilter() with { PageSize = pageSize });

        result.ShouldNotHaveValidationErrorFor(filter => filter.PageSize);
    }
}
