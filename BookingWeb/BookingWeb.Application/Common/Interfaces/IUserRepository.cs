using BookingWeb.Application.Models;

namespace BookingWeb.Application.Interfaces;

public interface IUserRepository
{
    Task<PagedResult<UserDto>> GetUsersAsync(UserFilterDto filter, CancellationToken ct = default);
}