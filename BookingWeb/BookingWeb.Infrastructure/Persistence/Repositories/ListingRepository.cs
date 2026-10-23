using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Listings;
using BookingWeb.Application.Listings.Requests;
using BookingWeb.Application.Models;
using BookingWeb.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingWeb.Infrastructure.Persistence.Repositories;

public sealed class ListingRepository : IListingRepository
{
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

    public Task<Listing?> GetByIdForHostAsync(Guid listingId, Guid hostId, CancellationToken ct = default)
    {
        return _db.Listings.FirstOrDefaultAsync(l => l.Id == listingId && l.HostId == hostId, ct);
    }

    public async Task<IReadOnlyList<Listing>> GetByHostAsync(Guid hostId, CancellationToken ct = default)
    {
        return await _db.Listings
            .AsNoTracking()
            .Where(l => l.HostId == hostId)
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<Listing>> SearchAsync(ListingFilterDto filter, CancellationToken ct = default)
    {
        var query = _db.Listings
            .AsNoTracking()
            .Where(l => l.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            var city = filter.City.Trim().ToLowerInvariant();
            query = query.Where(l => l.City.ToLower() == city);
        }

        if (filter.Guests is { } guests)
        {
            query = query.Where(l => l.MaxGuests >= guests);
        }
        
        var totalCount = await query.CountAsync(ct);
        
        var items = await  query
            .OrderByDescending(l => l.CreatedAtUtc)
            .ThenBy(l => l.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);
        
        return new PagedResult<Listing>(items, totalCount, filter.Page, filter.PageSize);
    }

    public void Add(Listing listing)
    {
        _db.Listings.Add(listing);
    }

    public Task DeactivateAllForHostAsync(Guid hostId, CancellationToken ct = default)
    {
        return _db.Listings
            .Where(listing => listing.HostId == hostId && listing.IsActive)
            .ExecuteUpdateAsync(setters => setters.SetProperty(listing => listing.IsActive, false), ct);
    }
}