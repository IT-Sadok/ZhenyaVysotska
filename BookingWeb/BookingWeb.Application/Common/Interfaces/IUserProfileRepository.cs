using BookingWeb.Domain.Models;

namespace BookingWeb.Application.Common.Interfaces;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    
    void Add(UserProfile profile);
    
    void Remove(UserProfile profile);
}