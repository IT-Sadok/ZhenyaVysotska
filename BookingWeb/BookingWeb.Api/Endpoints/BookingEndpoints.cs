using BookingWeb.Api.Extensions;
using BookingWeb.Application.Bookings;
using BookingWeb.Application.Bookings.Requests;
using BookingWeb.Application.Constants;
using BookingWeb.Application.Models;

namespace BookingWeb.Api.Endpoints;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/bookings")
            .WithTags("Bookings")
            .RequireAuthorization(PolicyNames.ClientOnly);

        group.MapPost("/", async (
            CreateBookingRequest request, HttpContext http, IBookingService bookings, CancellationToken ct) =>
        {
            var guestId = http.User.GetUserId();
            if (guestId is null) return Results.Unauthorized();

            var result = await bookings.CreateAsync(guestId.Value, request, ct);
            return result.Match(booking => Results.Created($"/api/bookings/{booking.Id}", booking));
        });

        group.MapGet("/", async (
            [AsParameters] PageRequest request, HttpContext http, IBookingService bookings, CancellationToken ct) =>
        {
            var guestId = http.User.GetUserId();
            if (guestId is null) return Results.Unauthorized();

            var result = await bookings.GetMineAsync(guestId.Value, request, ct);
            return result.Match(Results.Ok);
        });

        group.MapGet("/{id:guid}", async (
            Guid id, HttpContext http, IBookingService bookings, CancellationToken ct) =>
        {
            var guestId = http.User.GetUserId();
            if (guestId is null) return Results.Unauthorized();

            var result = await bookings.GetByIdAsync(guestId.Value, id, ct);
            return result.Match(Results.Ok);
        });
        
        group.MapPost("/{id:guid}/cancel", async (
            Guid id, HttpContext http, IBookingService bookings, CancellationToken ct) =>
        {
            var guestId = http.User.GetUserId();
            if (guestId is null) return Results.Unauthorized();

            var result = await bookings.CancelAsync(guestId.Value, id, ct);
            return result.Match(Results.Ok);
        });

        return routes;
    }
}
