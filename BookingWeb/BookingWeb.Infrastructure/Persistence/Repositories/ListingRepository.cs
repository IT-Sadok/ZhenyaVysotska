using BookingWeb.Application.Common.Interfaces;
using BookingWeb.Application.Listings;
using BookingWeb.Application.Models;
using BookingWeb.Domain.Entities;
using BookingWeb.Infrastructure.Persistence.Configurations;
using Mapster;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace BookingWeb.Infrastructure.Persistence.Repositories;

public sealed class ListingRepository : IListingRepository
{
    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(ListingConfiguration.Wgs84Srid);

    private readonly ApplicationDbContext _db;

    public ListingRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Listing?> GetByIdAsync(Guid listingId, CancellationToken ct = default)
    {
        return _db.Listings
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == listingId, ct);
    }

    public Task<PublicListingDto?> GetPublicByIdAsync(Guid listingId, CancellationToken ct = default)
    {
        return _db.Listings
            .AsNoTracking()
            .Where(l => l.Id == listingId && l.IsActive)
            .ProjectToType<PublicListingDto>()
            .FirstOrDefaultAsync(ct);
    }

    public Task<Listing?> GetByIdForHostAsync(Guid listingId, Guid hostId, CancellationToken ct = default)
    {
        return _db.Listings.FirstOrDefaultAsync(l => l.Id == listingId && l.HostId == hostId, ct);
    }

    public Task<PagedResult<HostListingDto>> GetByHostAsync(
        Guid hostId, PageRequest request, CancellationToken ct = default)
    {
        return _db.Listings
            .AsNoTracking()
            .Where(l => l.HostId == hostId)
            .OrderByDescending(l => l.CreatedAtUtc)
            .ThenBy(l => l.Id)
            .ProjectToType<HostListingDto>()
            .ToPagedResultAsync(request.Page, request.PageSize, ct);
    }

    public async Task<PagedResult<PublicListingDto>> SearchAsync(
        ListingSearchCriteria criteria, int page, int pageSize, CancellationToken ct = default)
    {
        var query = ApplyCriteria(criteria);

        var totalCount = await query.CountAsync(ct);

        var items = await OrderForSearch(query)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectToType<PublicListingDto>()
            .ToListAsync(ct);

        return new PagedResult<PublicListingDto>(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<ListingMarkerDto>> GetMarkersAsync(
        ListingSearchCriteria criteria, int take, CancellationToken ct = default)
    {
        return await OrderForSearch(ApplyCriteria(criteria))
            .Take(take)
            .ProjectToType<ListingMarkerDto>()
            .ToListAsync(ct);
    }

    public void Add(Listing listing) => _db.Listings.Add(listing);

    private IQueryable<Listing> ApplyCriteria(ListingSearchCriteria criteria)
    {
        var query = _db.Listings
            .AsNoTracking()
            .Where(l => l.IsActive);

        if (criteria.Guests is { } guests)
        {
            query = query.Where(l => l.MaxGuests >= guests);
        }

        if (criteria.MinPrice is { } minPrice)
        {
            query = query.Where(l => l.PricePerNight >= minPrice);
        }

        if (criteria.MaxPrice is { } maxPrice)
        {
            query = query.Where(l => l.PricePerNight <= maxPrice);
        }

        if (criteria.Area is { } area)
        {
            var box = GeometryFactory.ToGeometry(new Envelope(area.MinLng, area.MaxLng, area.MinLat, area.MaxLat));

            query = query.Where(l => EF.Property<Point>(l, ListingConfiguration.PublicGeom).Intersects(box));
        }

        if (criteria.Stay is { } stay)
        {
            query = query.Where(l => !_db.Bookings.Any(b =>
                b.ListingId == l.Id
                && b.Status == BookingStatus.Confirmed
                && PostgresFunctions.DateRange(b.CheckIn, b.CheckOut, PostgresFunctions.HalfOpen)
                    .Overlaps(PostgresFunctions.DateRange(stay.CheckIn, stay.CheckOut, PostgresFunctions.HalfOpen))));
        }

        return query;
    }
    
    private static IOrderedQueryable<Listing> OrderForSearch(IQueryable<Listing> query) =>
        query.OrderByDescending(l => l.CreatedAtUtc).ThenBy(l => l.Id);
}
