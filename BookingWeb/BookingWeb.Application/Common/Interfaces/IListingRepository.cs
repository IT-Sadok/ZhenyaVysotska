using BookingWeb.Application.Listings.Requests;
using BookingWeb.Application.Models;
using BookingWeb.Domain.Models;

namespace BookingWeb.Application.Common.Interfaces;

public interface IListingRepository
{
    void Add(Listing listing);
    
    Task<Listing?> GetByIdAsync(Guid listingId, CancellationToken ct = default);
    
    Task<Listing?> GetByIdForHostAsync(Guid listingId, Guid hostId, CancellationToken ct = default);

    Task<PagedResult<Listing>> GetByHostAsync(Guid hostId, PageRequest request,  CancellationToken ct = default);

    Task<PagedResult<Listing>> SearchAsync(ListingFilterDto filter, CancellationToken ct = default);
    
    Task DeactivateAllForHostAsync(Guid hostId, CancellationToken ct = default);
}