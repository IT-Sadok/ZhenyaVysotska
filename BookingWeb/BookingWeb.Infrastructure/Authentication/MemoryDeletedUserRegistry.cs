using BookingWeb.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BookingWeb.Infrastructure.Authentication;

public sealed class MemoryDeletedUserRegistry : IDeletedUserRegistry
{
    private readonly IMemoryCache _memoryCache;
    private readonly TimeSpan _entryLifeTime;

    public MemoryDeletedUserRegistry(IMemoryCache memoryCache, IOptions<JwtSettings> jwtSettings)
    {
        _memoryCache = memoryCache;
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
    
    private static string Key(Guid userId) => $"deleted-user:{userId:N}";
}