using BookingWeb.Application.Listings.Requests;
using BookingWeb.Application.Models;
using BookingWeb.Application.Results;

namespace BookingWeb.Application.Listings;

public interface IListingService
{
    Task<Result<ListingDto>> CreateAsync(Guid hostId, ListingRequest request, CancellationToken ct = default);
    
    Task<Result<ListingDto>> GetByIdAsync(Guid listingId, CancellationToken ct = default);

    Task<Result<PagedResult<ListingDto>>> GetMineAsync(Guid hostId, PageRequest request, CancellationToken ct = default);
    
    Task<Result<PagedResult<ListingDto>>> SearchAsync(ListingFilterDto filter, CancellationToken ct = default);

    Task<Result<ListingDto>> UpdateAsync(
        Guid hostId, Guid listingId, ListingRequest request, CancellationToken ct = default);

    Task<Result> DeleteAsync(Guid hostId, Guid listingId, CancellationToken ct = default);
}