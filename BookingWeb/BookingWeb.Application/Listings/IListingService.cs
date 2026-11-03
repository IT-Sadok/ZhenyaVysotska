using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BookingWeb.Application.Listings.Requests;
using BookingWeb.Application.Models;
using BookingWeb.Application.Results;

namespace BookingWeb.Application.Listings;

public interface IListingService
{
    Task<Result<HostListingDto>> CreateAsync(Guid hostId, ListingRequest request, CancellationToken ct = default);

    Task<Result<PublicListingDto>> GetByIdAsync(Guid listingId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<HostListingDto>>> GetMineAsync(Guid hostId, CancellationToken ct = default);

    Task<Result<PagedResult<PublicListingDto>>> SearchAsync(ListingFilterDto filter, CancellationToken ct = default);

    Task<Result<MapMarkersDto>> SearchInAreaAsync(MapSearchRequest request, CancellationToken ct = default);

    Task<Result<HostListingDto>> UpdateAsync(
        Guid hostId, Guid listingId, ListingRequest request, CancellationToken ct = default);

    Task<Result> DeleteAsync(Guid hostId, Guid listingId, CancellationToken ct = default);
}
