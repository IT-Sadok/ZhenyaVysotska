using BookingWeb.Application.Bookings;
using BookingWeb.Application.Listings;
using BookingWeb.Application.Profiles;
using BookingWeb.Domain.Entities;
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
            TypeAdapterConfig.GlobalSettings.CompileProjection();
            _globalRegistered = true;
        }
    }

    public static void RegisterMappings(TypeAdapterConfig config)
    {
        config.RequireDestinationMemberSource = true;

        config.NewConfig<Listing, PublicListingDto>()
            .Map(dest => dest.Latitude, src => src.PublicLocation.Latitude)
            .Map(dest => dest.Longitude, src => src.PublicLocation.Longitude);

        config.NewConfig<Listing, ListingMarkerDto>()
            .Map(dest => dest.Latitude, src => src.PublicLocation.Latitude)
            .Map(dest => dest.Longitude, src => src.PublicLocation.Longitude);

        config.NewConfig<Listing, HostListingDto>()
            .Map(dest => dest.Latitude, src => src.Location.Latitude)
            .Map(dest => dest.Longitude, src => src.Location.Longitude)
            .Map(dest => dest.PublicLatitude, src => src.PublicLocation.Latitude)
            .Map(dest => dest.PublicLongitude, src => src.PublicLocation.Longitude);

        config.NewConfig<UserProfile, ProfileDto>();

        // Status/CancelledBy are enums on the entity and strings in the DTO; Mapster maps them by name.
        config.NewConfig<Booking, BookingDto>();
    }
}
