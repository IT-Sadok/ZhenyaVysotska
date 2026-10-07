using BookingWeb.Application.Admin;
using BookingWeb.Application.Admin.Validators;
using BookingWeb.Application.Constants;
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
    private readonly Mock<IIdentityService> _identity = new();
    private readonly AdminService _sut;
    private static readonly PagedResult<UserDto> EmptyPage = new([], TotalCount: 0, Page: 1, PageSize: 20);
    
    public AdminServiceTests()
    {
        _sut = new AdminService(_identity.Object);
    }
    
    [Fact]
    public async Task GetUsers_ShouldReturnPageFromIdentityService_WhenFilterIsGiven()
    {
        var filter = new UserFilterDto(Role: Roles.Host, Email: null, SortBy: null);
        var page = new PagedResult<UserDto>(
            Items: [new UserDto(Guid.NewGuid(), "host@example.com", [Roles.Host])],
            TotalCount: 1,
            Page: 1,
            PageSize: 20);

        _identity
            .Setup(identity => identity.GetUsersAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var result = await _sut.GetUsersAsync(filter, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(page);
    }
    
    [Theory]
    [InlineData("host")]
    [InlineData("HOST")]
    [InlineData("Host")]
    public async Task GetUsers_ShouldPassCanonicalRoleName_WhenRoleHasAnyCase(string role)
    {
        var filter = new UserFilterDto(Role: role, Email: "test", SortBy: SortTokens.EmailAsc, Page: 2, PageSize: 10);
        
        _identity
            .Setup(identity => identity.GetUsersAsync(It.IsAny<UserFilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyPage);
        
        await _sut.GetUsersAsync(filter, CancellationToken.None);
        
        var expectedFilter = filter with { Role = Roles.Host };
        _identity.Verify(
            identity => identity.GetUsersAsync(expectedFilter, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
