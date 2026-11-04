using System.Reflection;
using BookingWeb.Application.Admin;
using BookingWeb.Application.Auth;
using BookingWeb.Application.Bookings;
using BookingWeb.Application.Common.Interfaces;
using BookingWeb.Application.Common.Mapping;
using BookingWeb.Application.Listings;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookingWeb.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        services.Scan(scan => scan
            .FromAssemblyOf<ListingService>()
            .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service")))
            .AsMatchingInterface()
            .WithScopedLifetime());

        services.TryAddSingleton(TimeProvider.System);
        
        services.AddSingleton<ILocationObfuscator, LocationObfuscator>();

        MappingConfig.RegisterMappings();
        return services;
    }
}