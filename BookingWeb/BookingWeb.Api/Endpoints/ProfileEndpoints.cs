using BookingWeb.Api.Extensions;
using BookingWeb.Application.Profiles;
using BookingWeb.Application.Profiles.Requests;

namespace BookingWeb.Api.Endpoints;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/profile")
            .WithTags("Profile")
            .RequireAuthorization();

        group.MapGet("/", async (HttpContext http, ProfileService profiles, CancellationToken ct) =>
        {
            var userId = http.User.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var result = await profiles.GetAsync(userId.Value, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        });

        group.MapPut("/", async (
            UpdateProfileRequest request, HttpContext http, ProfileService profiles, CancellationToken ct) =>
        {
            var userId = http.User.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var result = await profiles.UpdateAsync(userId.Value, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        });

        return app;
    }
}
