using BookingWeb.Application.Admin;
using BookingWeb.Application.Auth;
using BookingWeb.Application.Models;

namespace BookingWeb.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<PagedResult<UserDto>> GetUsersAsync(UserFilterDto filter, CancellationToken ct = default);
}