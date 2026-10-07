using BookingWeb.Application.Interfaces;
using BookingWeb.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingWeb.Infrastructure.Persistence;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _db;
    
    public RefreshTokenRepository(ApplicationDbContext db)
    {
        _db = db;
    }
    
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
    {
        return _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
    }

    public void Add(RefreshToken refreshToken)
    {
        _db.RefreshTokens.Add(refreshToken);
    }

    public Task RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedAtUtc, CancellationToken ct = default)
    {
        return _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.RevokedAtUtc, revokedAtUtc),
                ct);
    }
}