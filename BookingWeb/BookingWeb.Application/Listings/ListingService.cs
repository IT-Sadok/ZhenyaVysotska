using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Listings.Requests;
using BookingWeb.Application.Models;
using BookingWeb.Application.Results;
using BookingWeb.Domain.Entities;
using BookingWeb.Domain.ValueObjects;
using Mapster;

namespace BookingWeb.Application.Listings;

public sealed class ListingService : IListingService
{
    private readonly IListingRepository _listingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ILocationObfuscator _locationObfuscator;

    public ListingService(
        IListingRepository listingRepository, 
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILocationObfuscator locationObfuscator)
    {
        _listingRepository = listingRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _locationObfuscator = locationObfuscator;
    }
    
    public async Task<Result<HostListingDto>> CreateAsync(
        Guid hostId, ListingRequest request, CancellationToken ct = default)
    {
        var location = ToGeoPoint(request);

        var listing = Listing.Create(
            hostId,
            request.Title,
            request.Description,
            request.CountryCode,
            request.Region,
            request.City,
            request.Address,
            location: location,
            publicLocation: _locationObfuscator.Obfuscate(location),
            request.PricePerNight,
            request.MaxGuests,
            _timeProvider.GetUtcNow());
        
        _listingRepository.Add(listing);
        await _unitOfWork.SaveChangesAsync(ct);

        return listing.Adapt<HostListingDto>();
    }

    public async Task<Result<PublicListingDto>> GetByIdAsync(
        Guid listingId, CancellationToken ct = default)
    {
        var listing = await _listingRepository.GetPublicByIdAsync(listingId, ct);

        if (listing is null)
        {
            return ListingErrors.NotFound;
        }

        return listing;
    }

    public async Task<Result<IReadOnlyList<HostListingDto>>> GetMineAsync(
        Guid hostId, CancellationToken ct = default)
    {
        var listings = await _listingRepository.GetByHostAsync(hostId, ct);

        return Result.Success(listings);
    }

    public async Task<Result<PagedResult<PublicListingDto>>> SearchAsync(
        ListingFilterDto filter, CancellationToken ct = default)
    {
        return await _listingRepository.SearchAsync(
            ListingSearchCriteria.From(filter), filter.Page, filter.PageSize, ct);
    }

    public async Task<Result<MapMarkersDto>> SearchInAreaAsync(
        MapSearchRequest request, CancellationToken ct = default)
    {
        var criteria = ListingSearchCriteria.From(request);

        if (criteria.Area is null)
        {
            return ListingErrors.InvalidBoundingBox;
        }

        var markers = await _listingRepository.GetMarkersAsync(criteria, request.Limit + 1, ct);
        var isTruncated = markers.Count > request.Limit;

        return new MapMarkersDto(isTruncated ? markers.Take(request.Limit).ToList() : markers, isTruncated);
    }

    public async Task<Result<HostListingDto>> UpdateAsync(
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
            request.CountryCode,
            request.Region,
            request.City,
            request.Address,
            request.PricePerNight,
            request.MaxGuests);

        var location = ToGeoPoint(request);
        if (location != listing.Location)
        {
            listing.SetLocation(location, _locationObfuscator.Obfuscate(location));
        }
        
        await _unitOfWork.SaveChangesAsync(ct);

        return listing.Adapt<HostListingDto>();
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

    private static GeoPoint ToGeoPoint(ListingRequest request) =>
        new(request.Latitude!.Value, request.Longitude!.Value);
}
