using System.Security.Cryptography;
using BookingWeb.Application.Common.Settings;
using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Results;
using BookingWeb.Domain.Models;
using Microsoft.Extensions.Options;

namespace BookingWeb.Application.Auth;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private const int TokenSizeInBytes = 64;

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly RefreshTokenSettings _settings;
    private readonly TimeProvider _timeProvider;
    
    public RefreshTokenService(
        IRefreshTokenRepository refreshTokenRepository, 
        IOptions<RefreshTokenSettings> settings, 
        TimeProvider timeProvider)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _settings = settings.Value;
        _timeProvider = timeProvider;
    }

    public Task<RefreshTokenResult> IssueAsync(Guid userId, CancellationToken ct = default)
    {
        var rawToken = GenerateRawToken();
        var now = _timeProvider.GetUtcNow();
        var expiresAtUtc = now.AddDays(_settings.ExpiryDays);

        var refreshToken = RefreshToken.Create(userId, Hash(rawToken), now, expiresAtUtc);
        _refreshTokenRepository.Add(refreshToken);

        return Task.FromResult(new RefreshTokenResult(rawToken, expiresAtUtc));
    }

    public async Task<Result<Guid>> ValidateAsync(string token, CancellationToken ct = default)
    {
        var storedToken = await _refreshTokenRepository.GetByHashAsync(Hash(token), ct);
        if (storedToken is null)
        {
            return RefreshTokenErrors.Invalid;
        }
        
        var now = _timeProvider.GetUtcNow();

        if (storedToken.IsRevoked)
        {
            await _refreshTokenRepository.RevokeAllForUserAsync(storedToken.UserId, now, ct);
            return RefreshTokenErrors.Reused;
        }
        
        if (storedToken.IsExpiredAt(now))
        {
            return RefreshTokenErrors.Expired;
        }
        
        return storedToken.UserId;
    }

    public async Task RevokeAsync(string token, CancellationToken ct = default)
    {
        var storedToken = await _refreshTokenRepository.GetByHashAsync(Hash(token), ct);
        storedToken?.Revoke(_timeProvider.GetUtcNow());
    }

    public Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        return _refreshTokenRepository.RevokeAllForUserAsync(userId, _timeProvider.GetUtcNow(), ct);
    }

    private static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenSizeInBytes);   
        return Convert.ToBase64String(bytes);
    }

    private static string Hash(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
