using BookingWeb.Api.Extensions;
using BookingWeb.Application.Auth;
using BookingWeb.Application.Auth.Requests;
using BookingWeb.Application.Common.Interfaces;
using BookingWeb.Domain;

namespace BookingWeb.Api.Endpoints;

public sealed class AuthEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder routes)
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
                [AsParameters] AddRoleRequest request, IUserContext user, 
                IAuthService auth, CancellationToken ct) =>
        {
            var result = await auth.AddRoleAsync(user.UserId, request, ct);
            return result.Match(Results.Ok);
        })
            .RequireAuthorization();

        group.MapGet("/me", async (
            IUserContext user, IIdentityService identity, CancellationToken ct) =>
        {
            var result = await identity.GetUserByIdAsync(user.UserId, ct);
            return result.Match(Results.Ok);
        })
            .RequireAuthorization();
    }
}
