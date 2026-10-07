using BookingWeb.Application.Auth.Responses;
using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Models;
using BookingWeb.Application.Results;
using BookingWeb.Domain;
using FluentValidation;

namespace BookingWeb.Application.Admin;

public sealed class AdminService : IAdminService
{
    private readonly IIdentityService _identityService;

    public AdminService(IIdentityService identityService)
    {
        _identityService = identityService;
    }


    public async Task<Result<PagedResult<UserDto>>> GetUsersAsync(UserFilterDto filter, CancellationToken ct)
    {
        var normalizedFilter = filter with { Role = Roles.GetExactRoleName(filter.Role) };
        
        return await _identityService.GetUsersAsync(normalizedFilter, ct);
    }
}