namespace BookingWeb.Domain.Models;

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
        Guard.ThrowIfEmpty(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expiresAtUtc, createdAtUtc);
        
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