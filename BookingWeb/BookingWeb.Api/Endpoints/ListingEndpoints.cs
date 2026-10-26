using BookingWeb.Api.Extensions;
using BookingWeb.Application.Bookings;
using BookingWeb.Application.Constants;
using BookingWeb.Application.Listings;
using BookingWeb.Application.Listings.Requests;
using BookingWeb.Application.Models;

namespace BookingWeb.Api.Endpoints;

public static class ListingEndpoints
{
    public static IEndpointRouteBuilder MapListingEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/listings")
            .WithTags("Listings");

        group.MapGet("/", async (
            [AsParameters] ListingFilterDto filter, 
            IListingService service, CancellationToken ct = default) =>
        {
            var result = await service.SearchAsync(filter, ct);
            return result.Match(Results.Ok);
        });
        
        group.MapGet("/{id:guid}", async (
            Guid listingId, IListingService service, CancellationToken ct = default) =>
        {
            var result = await service.GetByIdAsync(listingId, ct);
            return result.Match(Results.Ok);
        });

        group.MapGet("/mine", async (
                [AsParameters] PageRequest request, HttpContext http, 
                IListingService listings, CancellationToken ct) =>
            {
                var hostId = http.User.GetUserId();
                if (hostId is null) return Results.Unauthorized();

                var result = await listings.GetMineAsync(hostId.Value, request, ct);
                return result.Match(Results.Ok);
            })
            .RequireAuthorization(PolicyNames.HostOnly);

        group.MapPost("/", async (
                ListingRequest request, HttpContext http, 
                IListingService listings, CancellationToken ct) =>
            {
                var hostId = http.User.GetUserId();
                if (hostId is null) return Results.Unauthorized();

                var result = await listings.CreateAsync(hostId.Value, request, ct);
                return result.Match(listing => Results.Created($"/api/listings/{listing.Id}", listing));
            })
            .RequireAuthorization(PolicyNames.HostOnly);

        group.MapPut("/{id:guid}", async (
                Guid id, ListingRequest request, HttpContext http, 
                IListingService listings, CancellationToken ct) =>
            {
                var hostId = http.User.GetUserId();
                if (hostId is null) return Results.Unauthorized();

                var result = await listings.UpdateAsync(hostId.Value, id, request, ct);
                return result.Match(Results.Ok);
            })
            .RequireAuthorization(PolicyNames.HostOnly);
        
        group.MapGet("/{id:guid}/bookings", async (
                Guid id, [AsParameters] PageRequest request, HttpContext http, 
                IBookingService bookings, CancellationToken ct) =>
            {
                var hostId = http.User.GetUserId();
                if (hostId is null) return Results.Unauthorized();

                var result = await bookings.GetForHostListingAsync(hostId.Value, id, request, ct);
                return result.Match(Results.Ok);
            })
            .RequireAuthorization(PolicyNames.HostOnly);

        group.MapPost("/{listingId:guid}/bookings/{bookingId:guid}/cancel", async (
                Guid listingId, Guid bookingId, HttpContext http, 
                IBookingService bookings, CancellationToken ct) =>
            {
                var hostId = http.User.GetUserId();
                if (hostId is null) return Results.Unauthorized();

                var result = await bookings.CancelByHostAsync(hostId.Value, listingId, bookingId, ct);
                return result.Match(Results.Ok);
            })
            .RequireAuthorization(PolicyNames.HostOnly);
        
        group.MapDelete("/{id:guid}", async (
                Guid id, HttpContext http, IListingService listings, CancellationToken ct) =>
            {
                var hostId = http.User.GetUserId();
                if (hostId is null) return Results.Unauthorized();

                var result = await listings.DeleteAsync(hostId.Value, id, ct);
                return result.Match(Results.NoContent);
            })
            .RequireAuthorization(PolicyNames.HostOnly);
        
        return routes;
    }
}