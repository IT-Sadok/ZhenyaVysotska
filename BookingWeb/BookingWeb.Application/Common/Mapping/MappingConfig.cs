using BookingWeb.Application.Listings;
using BookingWeb.Application.Profiles;
using BookingWeb.Domain.Models;
using Mapster;

namespace BookingWeb.Application.Common.Mapping;

public static class MappingConfig
{
    private static readonly Lock GlobalLock = new();
    private static bool _globalRegistered;
    
    public static void RegisterMappings()
    {
        lock (GlobalLock)
        {
            if (_globalRegistered)
            {
                return;
            }

            RegisterMappings(TypeAdapterConfig.GlobalSettings);
            TypeAdapterConfig.GlobalSettings.Compile();
            _globalRegistered = true;
        }
    }
    
    public static void RegisterMappings(TypeAdapterConfig config)
    {
        config.RequireDestinationMemberSource = true;

        config.NewConfig<Listing, ListingDto>();
        config.NewConfig<UserProfile, ProfileDto>();
    }
}
