using BookingWeb.Api.Extensions;
using BookingWeb.Application.Common.Interfaces;
using BookingWeb.Application.Profiles;
using BookingWeb.Application.Profiles.Requests;

namespace BookingWeb.Api.Endpoints;

public sealed class ProfileEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/profile")
            .WithTags("Profile")
            .RequireAuthorization();

        group.MapGet("/", async (
            IUserContext user, IProfileService profiles, CancellationToken ct) =>
        {
            var result = await profiles.GetAsync(user.UserId, ct);
            return result.Match(Results.Ok);
        });

        group.MapPut("/", async (
            UpdateProfileRequest request, IUserContext user, 
            IProfileService profiles, CancellationToken ct) =>
        {
            var result = await profiles.UpdateAsync(user.UserId, request, ct);
            return result.Match(Results.Ok);
        });

    }
}
