using BookingWeb.Api.Extensions;
using BookingWeb.Application.Auth;
using BookingWeb.Application.Auth.Requests;
using BookingWeb.Application.Interfaces;
using BookingWeb.Domain;

namespace BookingWeb.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/auth")
            .WithTags("Auth");

        group.MapPost("/register", async (
            RegisterRequest request, IAuthService auth, CancellationToken ct) =>
        {
            var result = await auth.RegisterAsync(request, ct);
            return result.Match(Results.Ok);
        });

        group.MapPost("/login", async (
            LoginRequest request, IAuthService auth, CancellationToken ct) =>
        {
            var result = await auth.LoginAsync(request, ct);
            return result.Match(Results.Ok);
        });
        
        group.MapPost("/refresh", async (
            RefreshRequest request, IAuthService auth, CancellationToken ct) =>
        {
            var result = await auth.RefreshAsync(request.RefreshToken, ct);
            return result.Match(Results.Ok);
        });

        group.MapPost("/logout", async (
            RefreshRequest request, IAuthService auth, CancellationToken ct) =>
        {
            await auth.LogoutAsync(request.RefreshToken, ct);
            return Results.NoContent();
        })
            .RequireAuthorization();

        group.MapPut("/me/roles/{role}", async (
                [AsParameters] AddRoleRequest request, HttpContext context, 
                IAuthService auth, CancellationToken ct) =>
        {
            var userId = context.User.GetUserId();
            if (userId is null) return Results.Unauthorized();
            
            var result = await auth.AddRoleAsync(userId.Value, request, ct);
            return result.Match(Results.Ok);
        })
            .RequireAuthorization();

        group.MapGet("/me", async (
            HttpContext context, IIdentityService identity, CancellationToken ct) =>
        {
            var userId = context.User.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var result = await identity.GetUserByIdAsync(userId.Value, ct);
            return result.Match(Results.Ok);
        })
            .RequireAuthorization();

        return routes;
    }
}
