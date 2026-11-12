using BookingWeb.Api.Extensions;
using BookingWeb.Application.Common.Interfaces;
using BookingWeb.Application.Bookings;
using BookingWeb.Application.Common.Constants;
using BookingWeb.Application.Listings;
using BookingWeb.Application.Listings.Requests;
using BookingWeb.Application.Models;

namespace BookingWeb.Api.Endpoints;

public sealed class ListingEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder routes)
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
        
        group.MapGet("/map", async (
            [AsParameters] MapSearchRequest request, IListingService service, CancellationToken ct = default) =>
        {
            var result = await service.SearchInAreaAsync(request, ct);
            return result.Match(Results.Ok);
        });

        group.MapGet("/{id:guid}", async (
            Guid id, IListingService service, CancellationToken ct = default) =>
        {
            var result = await service.GetByIdAsync(id, ct);
            return result.Match(Results.Ok);
        });

        group.MapGet("/mine", async (
                [AsParameters] PageRequest request, IUserContext user, 
                IListingService listings, CancellationToken ct) =>
            {
                var result = await listings.GetMineAsync(user.UserId, request, ct);
                return result.Match(Results.Ok);
            })
            .RequireAuthorization(PolicyNames.HostOnly);

        group.MapPost("/", async (
                ListingRequest request, IUserContext user, 
                IListingService listings, CancellationToken ct) =>
            {
                var result = await listings.CreateAsync(user.UserId, request, ct);
                return result.Match(listing => Results.Created($"/api/listings/{listing.Id}", listing));
            })
            .RequireAuthorization(PolicyNames.HostOnly);

        group.MapPut("/{id:guid}", async (
                Guid id, ListingRequest request, IUserContext user, 
                IListingService listings, CancellationToken ct) =>
            {
                var result = await listings.UpdateAsync(user.UserId, id, request, ct);
                return result.Match(Results.Ok);
            })
            .RequireAuthorization(PolicyNames.HostOnly);
        
        group.MapGet("/{id:guid}/bookings", async (
                Guid id, [AsParameters] PageRequest request, IUserContext user, 
                IBookingService bookings, CancellationToken ct) =>
            {
                var result = await bookings.GetForHostListingAsync(user.UserId, id, request, ct);
                return result.Match(Results.Ok);
            })
            .RequireAuthorization(PolicyNames.HostOnly);

        group.MapPost("/{listingId:guid}/bookings/{bookingId:guid}/cancel", async (
                Guid listingId, Guid bookingId, IUserContext user, 
                IBookingService bookings, CancellationToken ct) =>
            {
                var result = await bookings.CancelByHostAsync(user.UserId, listingId, bookingId, ct);
                return result.Match(Results.Ok);
            })
            .RequireAuthorization(PolicyNames.HostOnly);
        
        group.MapDelete("/{id:guid}", async (
                Guid id, IUserContext user, IListingService listings, CancellationToken ct) =>
            {
                var result = await listings.DeleteAsync(user.UserId, id, ct);
                return result.Match(Results.NoContent);
            })
            .RequireAuthorization(PolicyNames.HostOnly);
    }
}
