using BookingWeb.Application.Interfaces;
using BookingWeb.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingWeb.Infrastructure.Persistence;

public sealed class UserProfileRepository : IUserProfileRepository
{
    private readonly ApplicationDbContext _db;

    public UserProfileRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public void Add(UserProfile profile)
    {
        _db.UserProfiles.Add(profile);
    }
}
