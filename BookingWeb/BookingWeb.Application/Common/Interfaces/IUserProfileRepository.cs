using BookingWeb.Domain.Models;

namespace BookingWeb.Application.Interfaces;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}