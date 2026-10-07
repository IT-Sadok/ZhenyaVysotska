using BookingWeb.Application.Models;
using BookingWeb.Application.Results;

namespace BookingWeb.Application.Admin;

public interface IAdminService
{
    Task<Result<PagedResult<UserDto>>> GetUsersAsync(UserFilterDto filter, CancellationToken ct);
}