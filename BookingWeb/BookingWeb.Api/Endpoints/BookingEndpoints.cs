using BookingWeb.Api.Extensions;
using BookingWeb.Application.Common.Interfaces;
using BookingWeb.Application.Bookings;
using BookingWeb.Application.Bookings.Requests;
using BookingWeb.Application.Common.Constants;
using BookingWeb.Application.Models;

namespace BookingWeb.Api.Endpoints;

public sealed class BookingEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/bookings")
            .WithTags("Bookings")
            .RequireAuthorization(PolicyNames.ClientOnly);

        group.MapPost("/", async (
            CreateBookingRequest request, IUserContext user, IBookingService bookings, CancellationToken ct) =>
        {
            var result = await bookings.CreateAsync(user.UserId, request, ct);
            return result.Match(booking => Results.Created($"/api/bookings/{booking.Id}", booking));
        });

        group.MapGet("/", async (
            [AsParameters] PageRequest request, IUserContext user, IBookingService bookings, CancellationToken ct) =>
        {
            var result = await bookings.GetMineAsync(user.UserId, request, ct);
            return result.Match(Results.Ok);
        });

        group.MapGet("/{id:guid}", async (
            Guid id, IUserContext user, IBookingService bookings, CancellationToken ct) =>
        {
            var result = await bookings.GetByIdAsync(user.UserId, id, ct);
            return result.Match(Results.Ok);
        });
        
        group.MapPost("/{id:guid}/cancel", async (
            Guid id, IUserContext user, IBookingService bookings, CancellationToken ct) =>
        {
            var result = await bookings.CancelAsync(user.UserId, id, ct);
            return result.Match(Results.Ok);
        });
    }
}
