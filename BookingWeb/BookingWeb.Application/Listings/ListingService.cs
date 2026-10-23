using System.Reflection;
using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Listings.Requests;
using BookingWeb.Application.Models;
using BookingWeb.Application.Results;
using BookingWeb.Domain.Models;

namespace BookingWeb.Application.Listings;

public sealed class ListingService : IListingService
{
    private readonly IListingRepository _listingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ListingService(
        IListingRepository listingRepository, 
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _listingRepository = listingRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    
    public async Task<Result<ListingDto>> CreateAsync(
        Guid hostId, ListingRequest request, CancellationToken ct = default)
    {
        var listing = Listing.Create(
            hostId,
            request.Title,
            request.Description,
            request.City,
            request.Address,
            request.PricePerNight,
            request.MaxGuests,
            _timeProvider.GetUtcNow());
        
        _listingRepository.Add(listing);
        await _unitOfWork.SaveChangesAsync(ct);

        return listing.ToDto();
    }

    public async Task<Result<ListingDto>> GetByIdAsync(
        Guid listingId, CancellationToken ct = default)
    {
        var listing = await _listingRepository.GetByIdAsync(listingId, ct);

        if (listing is null || !listing.IsActive)
        {
            return ListingErrors.NotFound;
        }

        return listing.ToDto();
    }

    public async Task<Result<IReadOnlyList<ListingDto>>> GetMineAsync(
        Guid hostId, CancellationToken ct = default)
    {
        var listing = await _listingRepository.GetByHostAsync(hostId, ct);
        IReadOnlyList<ListingDto> items = listing.Select(l => l.ToDto()).ToList();

        return Result.Success(items);
    }

    public async Task<Result<PagedResult<ListingDto>>> SearchAsync(
        ListingFilterDto filter, CancellationToken ct = default)
    {
        var page = await _listingRepository.SearchAsync(filter, ct);
        var items = page.Items.Select(l=> l.ToDto()).ToList();
        
        return new PagedResult<ListingDto>(items, page.TotalCount, page.Page, page.PageSize);
    }

    public async Task<Result<ListingDto>> UpdateAsync(
        Guid hostId, Guid listingId, ListingRequest request, CancellationToken ct = default)
    {
        var listing = await _listingRepository.GetByIdForHostAsync(listingId, hostId, ct);
        if (listing is null)
        {
            return ListingErrors.NotFound;
        }

        listing.Update(
            request.Title,
            request.Description,
            request.City,
            request.Address,
            request.PricePerNight,
            request.MaxGuests);
        
        await _unitOfWork.SaveChangesAsync(ct);

        return listing.ToDto();
    }

    public async Task<Result> DeleteAsync(
        Guid hostId, Guid listingId, CancellationToken ct = default)
    {
        var listing = await _listingRepository.GetByIdForHostAsync(listingId, hostId, ct);
        if (listing is null)
        {
            return ListingErrors.NotFound;
        }
        
        listing.Deactivate();
        await _unitOfWork.SaveChangesAsync(ct);
        
        return Result.Success();
    }
}