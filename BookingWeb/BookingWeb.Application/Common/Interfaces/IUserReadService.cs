using BookingWeb.Application.Models;

namespace BookingWeb.Application.Interfaces;

public interface IUserReadService
{
    Task<PagedResult<UserDto>> GetUsersAsync(UserFilterDto filter, CancellationToken ct);
}