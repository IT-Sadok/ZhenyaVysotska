using BookingWeb.Application.Results;

namespace BookingWeb.Application.Interfaces;

public interface IRefreshTokenService
{
    Task<RefreshTokenResult> IssueAsync(Guid userId, CancellationToken ct = default);
    
    Task<Result<Guid>> ValidateAsync(string token, CancellationToken ct = default );
    
    Task RevokeAsync(string token, CancellationToken ct = default);
    
    Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);
}

public sealed record RefreshTokenResult(string Token, DateTimeOffset ExpiresAtUtc);