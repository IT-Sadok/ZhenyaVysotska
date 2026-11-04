using Ardalis.GuardClauses;

namespace BookingWeb.Domain.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    
    private RefreshToken() { }

    public static RefreshToken Create(
        Guid userId, string tokenHash, DateTimeOffset createdAtUtc, DateTimeOffset expiresAtUtc)
    {
        Guard.Against.Default(userId);
        Guard.Against.NullOrWhiteSpace(tokenHash);
        Guard.Against.Expression(
            expiresAt => expiresAt <= createdAtUtc,
            expiresAtUtc,
            "Expiry must be after creation time.");
        
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId =  userId,
            TokenHash = tokenHash,
            CreatedAtUtc = createdAtUtc,
            ExpiresAtUtc =  expiresAtUtc
        };
    }
    
    public bool IsRevoked => RevokedAtUtc is not null;
    
    public bool IsExpiredAt(DateTimeOffset now) => ExpiresAtUtc <= now;

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAtUtc = revokedAtUtc;
    }
}