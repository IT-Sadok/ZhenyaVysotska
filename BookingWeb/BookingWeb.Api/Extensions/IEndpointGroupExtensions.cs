using System.Reflection;
using BookingWeb.Api.Endpoints;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookingWeb.Api.Extensions;

public static class IEndpointGroupExtensions
{
    public static IServiceCollection AddEndpointGroups(
        this IServiceCollection services, Assembly assembly)
    {
        var descriptors = assembly.DefinedTypes
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && t.IsAssignableTo(typeof(IEndpointGroup)))
            .Select(t => ServiceDescriptor.Transient(typeof(IEndpointGroup), t));
        
        services.TryAddEnumerable(descriptors);
        
        return services;
    }

    public static IEndpointRouteBuilder MapEndpointGroups(this IEndpointRouteBuilder routes)
    {
        var groups = routes.ServiceProvider.GetRequiredService<IEnumerable<IEndpointGroup>>();

        foreach (var group in groups)
        {
            group.MapEndpoints(routes);
        }
        
        return routes;
    }
}