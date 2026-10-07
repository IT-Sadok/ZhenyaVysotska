using BookingWeb.Domain.Models;

namespace BookingWeb.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);

    void Add(RefreshToken refreshToken);

    Task RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedAtUtc, CancellationToken ct = default);
}