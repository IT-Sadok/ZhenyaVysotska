using System.Security.Cryptography;
using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Results;
using BookingWeb.Infrastructure.Identity;
using BookingWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookingWeb.Infrastructure.Authentication;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDbContext _db;
    private readonly JwtSettings _settings;

    public RefreshTokenService(ApplicationDbContext db, IOptions<JwtSettings> settings)
    {
        _db = db;
        _settings = settings.Value;
    }

    public async Task<RefreshTokenResult> IssueAsync(Guid userId, CancellationToken ct = default)
    {
        var raw = GenerateRawToken();                 
        var expiresAt = DateTimeOffset.UtcNow.AddDays(_settings.RefreshTokenExpiryDays);

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(raw),                     
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = expiresAt
        });
        await _db.SaveChangesAsync(ct);

        return new RefreshTokenResult(raw, expiresAt);
    }

    public async Task<Result<Guid>> ValidateAsync(string token, CancellationToken ct = default)
    {
        var hash = Hash(token);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null)
            return Result.Failure<Guid>(new Error("Auth.RefreshInvalid", "Refresh token is invalid."));

        if (stored.RevokedAtUtc is not null)
        {
            await RevokeAllForUserAsync(stored.UserId, ct);
            return Result.Failure<Guid>(new Error("Auth.RefreshReused", "Token reused - all sessions reset."));
        }

        if (stored.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            return Result.Failure<Guid>(new Error("Auth.RefreshExpired", "Refresh token is expired."));

        return Result.Success(stored.UserId);
    }

    public async Task RevokeAsync(string token, CancellationToken ct = default)
    {
        var hash = Hash(token);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is { RevokedAtUtc: null })
        {
            stored.RevokedAtUtc = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.RevokedAtUtc, 
                    DateTimeOffset.UtcNow), ct);
    }

    private static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);   
        return Convert.ToBase64String(bytes);
    }

    private static string Hash(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
