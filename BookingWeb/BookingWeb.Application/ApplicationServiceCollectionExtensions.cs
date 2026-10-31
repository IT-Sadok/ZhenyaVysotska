using System.Reflection;
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

        MappingConfig.RegisterMappings();
        return services;
    }
}