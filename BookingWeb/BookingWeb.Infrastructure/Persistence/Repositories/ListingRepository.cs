using BookingWeb.Application.Common.Interfaces;
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
    
    public void Add(Listing listing)
    {
        _db.Listings.Add(listing);
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

    public Task<PagedResult<Listing>> GetByHostAsync(Guid hostId, PageRequest request, CancellationToken ct = default)
    {
        return _db.Listings
            .AsNoTracking()
            .OrderByDescending(l => l.CreatedAtUtc)
            .ThenBy(l => l.Id)
            .ToPagedResultAsync(request.Page, request.PageSize, ct);
    }
    
    public Task<PagedResult<Listing>> SearchAsync(ListingFilterDto filter, CancellationToken ct = default)
    {
        var query = _db.Listings
            .AsNoTracking()
            .Where(l => l.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            var city = filter.City.Trim().ToLowerInvariant();
            query = query.Where(l => l.City.ToLower() == city);
        }

        if (filter.Guests is int guests)
        {
            query = query.Where(l => l.MaxGuests >= guests);
        }

        if (filter.CheckIn is DateOnly checkIn && filter.CheckOut is DateOnly checkOut)
        {
            var bookedListingIds = _db.Bookings
                .ThatBlock(checkIn, checkOut)
                .Select(b => b.ListingId);
            
            query = query.Where(l => !bookedListingIds.Contains(l.Id));
        }
        
        return query
            .OrderByDescending(l => l.CreatedAtUtc)
            .ThenBy(l=>l.Id)
            .ToPagedResultAsync(filter.Page, filter.PageSize, ct);
    }

    public Task DeactivateAllForHostAsync(Guid hostId, CancellationToken ct = default)
    {
        return _db.Listings
            .Where(listing => listing.HostId == hostId && listing.IsActive)
            .ExecuteUpdateAsync(setters => setters.SetProperty(listing => listing.IsActive, false), ct);
    }
}