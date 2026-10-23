using BookingWeb.Application.Listings;
using BookingWeb.Application.Listings.Requests;
using BookingWeb.Application.Models;
using BookingWeb.Domain.Models;

namespace BookingWeb.Application.Interfaces;

public interface IListingRepository
{
    Task<Listing?> GetByIdAsync(Guid listingId, CancellationToken ct = default);
    
    Task<Listing?> GetByIdForHostAsync(Guid listingId, Guid hostId, CancellationToken ct = default);

    Task<IReadOnlyList<Listing>> GetByHostAsync(Guid hostId, CancellationToken ct = default);

    Task<PagedResult<Listing>> SearchAsync(ListingFilterDto filter, CancellationToken ct = default);

    void Add(Listing listing);
    
    Task DeactivateAllForHostAsync(Guid hostId, CancellationToken ct = default);
}