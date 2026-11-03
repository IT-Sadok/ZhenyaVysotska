using BookingWeb.Application.Listings;
using BookingWeb.Application.Models;
using BookingWeb.Domain.Entities;

namespace BookingWeb.Application.Common.Interfaces;

public interface IListingRepository
{
    /// <summary>Untracked entity lookup (any status); used by bookings to check host, capacity and price.</summary>
    Task<Listing?> GetByIdAsync(Guid listingId, CancellationToken ct = default);

    Task<PublicListingDto?> GetPublicByIdAsync(Guid listingId, CancellationToken ct = default);
    
    Task<Listing?> GetByIdForHostAsync(Guid listingId, Guid hostId, CancellationToken ct = default);

    Task<PagedResult<HostListingDto>> GetByHostAsync(Guid hostId, PageRequest request, CancellationToken ct = default);

    Task<PagedResult<PublicListingDto>> SearchAsync(
        ListingSearchCriteria criteria, int page, int pageSize, CancellationToken ct = default);
    
    Task<IReadOnlyList<ListingMarkerDto>> GetMarkersAsync(
        ListingSearchCriteria criteria, int take, CancellationToken ct = default);

    void Add(Listing listing);

    Task DeactivateAllForHostAsync(Guid hostId, CancellationToken ct = default);
}
