using BookingWeb.Application.Admin;
using BookingWeb.Application.Admin.Validators;
using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Models;
using BookingWeb.Application.Results;
using BookingWeb.Domain;
using FluentValidation;
using Moq;
using Shouldly;

namespace BookingWeb.UnitTests.Admin;

public class AdminServiceTests
{
    private readonly Mock<IUserReadService> _userRead = new();
    
    private AdminService CreateSut() => new(
        _userRead.Object,
        new IValidator<UserFilterDto>[] { new UserFilterDtoValidator() });

    private static PagedResult<UserDto> EmptyPage(UserFilterDto f) =>
        new(Array.Empty<UserDto>(), 0, f.Page, f.PageSize);

    [Fact]
    public async Task Valid_filter_calls_read_and_returns_success()
    {
        var filter = new UserFilterDto(Role: Roles.Host, Email: null, SortBy: null);
        var page = new PagedResult<UserDto>(
            new[] { new UserDto(Guid.NewGuid(), "host@example.com", new[] { Roles.Host }, Roles.Host) },
            TotalCount: 1, Page: 1, PageSize: 20);

        _userRead.Setup(r => r.GetUsersAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var result = await CreateSut().GetUsersAsync(filter, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Items.Count.ShouldBe(1);
        _userRead.Verify(r => r.GetUsersAsync(filter, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Invalid_role_fails_validation_and_skips_read()
    {
        var filter = new UserFilterDto(Role: "Superuser", Email: null, SortBy: null);

        var result = await CreateSut().GetUsersAsync(filter, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<ValidationError>();
        _userRead.Verify(r => r.GetUsersAsync(It.IsAny<UserFilterDto>(),
            It.IsAny<CancellationToken>()), Times.Never); 
    }

    [Fact]
    public async Task PageSize_over_limit_fails_validation_and_skips_read()
    {
        var filter = new UserFilterDto(Role: null, Email: null, SortBy: null, Page: 1, PageSize: 500);

        var result = await CreateSut().GetUsersAsync(filter, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<ValidationError>();
        _userRead.Verify(r => r.GetUsersAsync(It.IsAny<UserFilterDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Empty_filter_is_valid_and_returns_page()
    {
        var filter = new UserFilterDto(Role: null, Email: null, SortBy: null);
        _userRead.Setup(r => r.GetUsersAsync(It.IsAny<UserFilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyPage(filter));

        var result = await CreateSut().GetUsersAsync(filter, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();
    }
}
