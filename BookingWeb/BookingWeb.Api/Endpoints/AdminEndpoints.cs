using BookingWeb.Api.Extensions;
using BookingWeb.Application.Admin;
using BookingWeb.Application.Constants;
using BookingWeb.Application.Models;
using BookingWeb.Application.Results;

namespace BookingWeb.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("admin")
            .WithTags("Admin")
            .RequireAuthorization(PolicyNames.AdminOnly);

        group.MapGet("/users", async (
            [AsParameters] UserFilterDto filter, IAdminService service, CancellationToken ct = default) =>
        {
            var result = await service.GetUsersAsync(filter, ct);

            return result.Match(Results.Ok);
        });
        
        return routes;
    }
}