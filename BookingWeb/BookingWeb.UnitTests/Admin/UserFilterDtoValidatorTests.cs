using BookingWeb.Application.Admin.Validators;
using BookingWeb.Application.Constants;
using BookingWeb.Application.Models;
using FluentValidation.TestHelper;

namespace BookingWeb.UnitTests.Admin;

public class UserFilterDtoValidatorTests
{
    private readonly UserFilterDtoValidator _validator = new();
    
    private static UserFilterDto Valid() => new(Role: null, Email: null, SortBy: null, Page: 1, PageSize: 20);

    [Fact]
    public void Empty_filter_passes()
    {
        var result = _validator.TestValidate(Valid());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_role_is_treated_as_absent(string role)
    {
        var result = _validator.TestValidate(Valid() with { Role = role });
        result.ShouldNotHaveValidationErrorFor(x => x.Role);
    }

    [Fact]
    public void Unknown_role_fails()
    {
        var result = _validator.TestValidate(Valid() with { Role = "Superuser" });
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }

    [Theory]
    [InlineData("Client")]
    [InlineData("Host")]
    [InlineData("Admin")] 
    public void Known_roles_pass(string role)
    {
        var result = _validator.TestValidate(Valid() with { Role = role });
        result.ShouldNotHaveValidationErrorFor(x => x.Role);
    }

    [Fact]
    public void Unknown_sort_token_fails()
    {
        var result = _validator.TestValidate(Valid() with { SortBy = "email" });
        result.ShouldHaveValidationErrorFor(x => x.SortBy);
    }

    [Fact]
    public void Known_sort_tokens_pass()
    {
        _validator.TestValidate(Valid() with { SortBy = SortTokens.EmailAsc })
            .ShouldNotHaveValidationErrorFor(x => x.SortBy);
        _validator.TestValidate(Valid() with { SortBy = SortTokens.EmailDesc })
            .ShouldNotHaveValidationErrorFor(x => x.SortBy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Page_below_one_fails(int page)
    {
        var result = _validator.TestValidate(Valid() with { Page = page });
        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]     
    [InlineData(1000000)] 
    public void PageSize_out_of_range_fails(int pageSize)
    {
        var result = _validator.TestValidate(Valid() with { PageSize = pageSize });
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(100)] 
    public void PageSize_in_range_passes(int pageSize)
    {
        var result = _validator.TestValidate(Valid() with { PageSize = pageSize });
        result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
    }
}
