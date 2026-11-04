using BookingWeb.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BookingWeb.Infrastructure.Authentication;

/// <summary>
/// Remembers deleted users until their last access token expires, so the JWT handler can reject it.
/// Uses its own MemoryCache instead of the shared IMemoryCache from DI: the shared one has a
/// SizeLimit (geocoding), and a size-limited cache may silently skip or evict entries.
/// Losing an entry here would let a deleted user's token work again.
/// </summary>
public sealed class MemoryDeletedUserRegistry : IDeletedUserRegistry, IDisposable
{
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly TimeSpan _entryLifeTime;

    public MemoryDeletedUserRegistry(IOptions<JwtSettings> jwtSettings)
    {
        _entryLifeTime = TimeSpan.FromMinutes(jwtSettings.Value.ExpiryMinutes)
                         + TokenValidationParameters.DefaultClockSkew;
    }
    
    public void Add(Guid userId)
    {
        _memoryCache.Set(Key(userId), true, _entryLifeTime);
    }

    public bool IsDeleted(Guid userId)
    {
        return _memoryCache.TryGetValue(Key(userId), out _);
    }

    // The DI container disposes singletons on shutdown.
    public void Dispose()
    {
        _memoryCache.Dispose();
    }
    
    private static string Key(Guid userId) => $"deleted-user:{userId:N}";
}
