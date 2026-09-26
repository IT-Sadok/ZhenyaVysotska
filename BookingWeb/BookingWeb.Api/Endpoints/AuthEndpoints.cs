using BookingWeb.Api.Extensions;
using BookingWeb.Application.Auth;
using BookingWeb.Application.Auth.Requests;
using BookingWeb.Application.Interfaces;

namespace BookingWeb.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/register", async (
            RegisterRequest request, AuthService auth, CancellationToken ct) =>
        {
            var result = await auth.RegisterAsync(request, ct);
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblem();
        });

        group.MapPost("/login", async (
            LoginRequest request, AuthService auth, CancellationToken ct) =>
        {
            var result = await auth.LoginAsync(request, ct);
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblem();
        });
        
        group.MapPost("/refresh", async (
            RefreshRequest request, AuthService auth, CancellationToken ct) =>
        {
            var result = await auth.RefreshAsync(request.RefreshToken, ct);
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblem();
        });

        group.MapPost("/logout", async (
            RefreshRequest request, IRefreshTokenService refreshTokens, CancellationToken ct) =>
        {
            await refreshTokens.RevokeAsync(request.RefreshToken, ct);
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapPost("/enable-persona", async (
            EnablePersonaRequest request, HttpContext http, AuthService auth, CancellationToken ct) =>
        {
            var userId = http.User.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var result = await auth.EnablePersonaAsync(userId.Value, request.Persona, ct);
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblem();
        }).RequireAuthorization();

        group.MapPost("/switch-persona", async (
            SwitchPersonaRequest request, HttpContext http, AuthService auth, CancellationToken ct) =>
        {
            var userId = http.User.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var result = await auth.SwitchPersonaAsync(userId.Value, request.Persona, ct);
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblem();
        }).RequireAuthorization();

        group.MapGet("/me", async (
            HttpContext http, IIdentityService identity, CancellationToken ct) =>
        {
            var userId = http.User.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var result = await identity.GetActiveUserAsync(userId.Value, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        }).RequireAuthorization();

        return app;
    }
}
