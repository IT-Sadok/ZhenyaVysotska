using BookingWeb.Api.Extensions;
using BookingWeb.Application.Admin;
using BookingWeb.Application.Constants;
using BookingWeb.Application.Models;
using BookingWeb.Application.Results;

namespace BookingWeb.Api.Endpoints;

public sealed class AdminEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder routes)
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
    }
}