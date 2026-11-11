using BookingWeb.Application.Common.Interfaces;
using BookingWeb.Application.Listings;
using BookingWeb.Application.Listings.Requests;
using BookingWeb.Application.Models;
using BookingWeb.Domain.Entities;
using BookingWeb.Domain.ValueObjects;
using BookingWeb.UnitTests.TestDoubles;
using Moq;
using Shouldly;

namespace BookingWeb.UnitTests.Listings;

public sealed class ListingServiceTests
{
    private static readonly DateTimeOffset Now = new (2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly GeoPoint KyivExact = new(50.4501, 30.5234);
    private static readonly GeoPoint KyivPublic = new(50.4530, 30.5260);
    private static readonly GeoPoint OdesaExact = new(46.4825, 30.7233);

    private readonly Guid _hostId = Guid.NewGuid();
    private readonly Mock<IListingRepository> _listings = new();
    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FakeLocationObfuscator _obfuscator = new();
    private readonly ListingService _sut;

    public ListingServiceTests()
    {
        _sut = new ListingService(_listings.Object, _bookings.Object, _unitOfWork.Object, new FixedTimeProvider(Now), _obfuscator);
    }

    private Listing NewListing(
        string title = "Loft", string? description = null, decimal price = 1000m, int guests = 2)
    {
        return Listing.Create(
            _hostId, title, description, "Kyiv", "Kyiv", "Khreshchatyk 1",
            location: KyivExact, publicLocation: KyivPublic, price, guests, Now);
    }

    private Listing CreateOwnListing()
    {
        var listing = NewListing();
        _listings
            .Setup(repository => repository.
                GetByIdForHostAsync(listing.Id, _hostId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listing);
        return listing;
    }

    private static ListingRequest CreateRequest()
    {
        return new ListingRequest(
            "Sea view", "Near the beach", "Odesa Oblast", "Odesa", "Prymorska 1",
            OdesaExact.Latitude, OdesaExact.Longitude, 2000m, 4);
    }

    private static PublicListingDto PublicDto(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), Guid.NewGuid(), "Loft", null, null, "Kyiv",
            KyivPublic.Latitude, KyivPublic.Longitude, 1000m, 2);

    private static List<ListingMarkerDto> Markers(int count) =>
        Enumerable.Range(0, count)
            .Select(i => new ListingMarkerDto(Guid.NewGuid(), $"Loft {i}", 1000m, 50.45, 30.52))
            .ToList();

    private static MapSearchRequest MapRequest(int limit = 200) =>
        new(50.3, 30.3, 50.6, 30.8, MinPrice: 500m, MaxPrice: 3000m, Guests: 2, Limit: limit);
    

    [Fact]
    public async Task Create_ShouldAddListingOwnedByHost_WhenRequestIsValid()
    {
        var result = await _sut.CreateAsync(_hostId, CreateRequest());

        result.IsSuccess.ShouldBeTrue();
        result.Value.HostId.ShouldBe(_hostId);
        result.Value.IsActive.ShouldBeTrue();
        _listings.Verify(repository => repository.Add(It.Is<Listing>(listing => listing.HostId == _hostId)), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ShouldStoreExactAndObfuscatedLocation_WhenRequestIsValid()
    {
        Listing? added = null;
        _listings.Setup(repository => repository.Add(It.IsAny<Listing>())).Callback<Listing>(l => added = l);

        await _sut.CreateAsync(_hostId, CreateRequest());

        _obfuscator.Calls.ShouldBe([OdesaExact]);
        added.ShouldNotBeNull();
        added.Location.ShouldBe(OdesaExact);
        added.PublicLocation.ShouldBe(
            new GeoPoint(OdesaExact.Latitude + FakeLocationObfuscator.LatitudeOffset, OdesaExact.Longitude));
        added.Region.ShouldBe("Odesa Oblast");
    }

    [Fact]
    public async Task Create_ShouldReturnHostDtoWithAddressAndExactLocation_WhenRequestIsValid()
    {
        var result = await _sut.CreateAsync(_hostId, CreateRequest());

        result.Value.Address.ShouldBe("Prymorska 1");
        result.Value.Latitude.ShouldBe(OdesaExact.Latitude);
        result.Value.Longitude.ShouldBe(OdesaExact.Longitude);
        result.Value.PublicLatitude.ShouldBe(OdesaExact.Latitude + FakeLocationObfuscator.LatitudeOffset);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFoundWithoutSaving_WhenRepositoryFindsNoListingForHost()
    {
        var result = await _sut.UpdateAsync(_hostId, Guid.NewGuid(), CreateRequest());

        result.Error.ShouldBe(ListingErrors.NotFound);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_ShouldReplaceFields_WhenListingBelongsToHost()
    {
        var listing = CreateOwnListing();

        var result = await _sut.UpdateAsync(_hostId, listing.Id, CreateRequest());

        result.IsSuccess.ShouldBeTrue();
        result.Value.Address.ShouldBe("Prymorska 1");
        listing.City.ShouldBe("Odesa");
        listing.PricePerNight.ShouldBe(2000m);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_ShouldKeepPublicLocationAndNotObfuscate_WhenCoordinatesAreUnchanged()
    {
        var listing = CreateOwnListing();
        var request = CreateRequest() with { Latitude = KyivExact.Latitude, Longitude = KyivExact.Longitude };

        await _sut.UpdateAsync(_hostId, listing.Id, request);

        _obfuscator.Calls.ShouldBeEmpty();
        listing.Location.ShouldBe(KyivExact);
        listing.PublicLocation.ShouldBe(KyivPublic);
        listing.Title.ShouldBe("Sea view");
    }

    [Fact]
    public async Task Update_ShouldRegeneratePublicLocation_WhenCoordinatesChanged()
    {
        var listing = CreateOwnListing();

        await _sut.UpdateAsync(_hostId, listing.Id, CreateRequest());

        _obfuscator.Calls.ShouldBe([OdesaExact]);
        listing.Location.ShouldBe(OdesaExact);
        listing.PublicLocation.ShouldNotBe(KyivPublic);
        listing.PublicLocation.ShouldBe(
            new GeoPoint(OdesaExact.Latitude + FakeLocationObfuscator.LatitudeOffset, OdesaExact.Longitude));
    }

    [Fact]
public async Task Delete_ShouldReturnConflictAndKeepActive_WhenListingHasUpcomingBookings()
    {
        var listing = CreateOwnListing();
        _bookings
            .Setup(repository => repository.HasUpcomingForListingAsync(listing.Id, DateOnly.FromDateTime(Now.UtcDateTime), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.DeleteAsync(_hostId, listing.Id);

        result.Error.ShouldBe(ListingErrors.HasUpcomingBookings);
        listing.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Delete_ShouldCheckUpcomingBookingsAgainstKyivDate_WhenUtcDateIsStillYesterday()
    {
        var lateEveningUtc = new DateTimeOffset(2026, 2, 28, 22, 30, 0, TimeSpan.Zero);
        var sut = new ListingService(
            _listings.Object, _bookings.Object, _unitOfWork.Object, new FixedTimeProvider(lateEveningUtc), _obfuscator);
        var listing = CreateOwnListing();
        var kyivToday = new DateOnly(2026, 3, 1);

        await sut.DeleteAsync(_hostId, listing.Id);

        _bookings.Verify(
            repository => repository.HasUpcomingForListingAsync(listing.Id, kyivToday, It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task Delete_ShouldDeactivateListing_WhenThereAreNoUpcomingBookings()
    {
        var listing = CreateOwnListing();

        var result = await _sut.DeleteAsync(_hostId, listing.Id);

        result.IsSuccess.ShouldBeTrue();
        listing.IsActive.ShouldBeFalse();
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Reads ----------

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenRepositoryFindsNoActiveListing()
    {
        _listings
            .Setup(repository => repository.GetPublicByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PublicListingDto?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Error.ShouldBe(ListingErrors.NotFound);
    }

    [Fact]
    public async Task GetById_ShouldReturnPublicDto_WhenListingIsActive()
    {
        var dto = PublicDto();
        _listings
            .Setup(repository => repository.GetPublicByIdAsync(dto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _sut.GetByIdAsync(dto.Id);

        result.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task GetMine_ShouldReturnEmptyPage_WhenHostHasNoListings()
    {
        var request = new PageRequest(Page: 2, PageSize: 5);
        _listings
            .Setup(repository => repository.GetByHostAsync(_hostId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<HostListingDto>([], 0, 2, 5));

        var result = await _sut.GetMineAsync(_hostId, request);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
        result.Value.Page.ShouldBe(2);
        result.Value.PageSize.ShouldBe(5);
    }

    [Fact]
    public async Task Search_ShouldPassFiltersAsCriteriaAndPaging_WhenFilterHasBoundingBox()
    {
        var filter = new ListingFilterDto(
            Guests: 2, MinPrice: 500m, MaxPrice: 3000m,
            MinLat: 50.3, MinLng: 30.3, MaxLat: 50.6, MaxLng: 30.8, Page: 3, PageSize: 10);
        var expected = new ListingSearchCriteria(2, 500m, 3000m, new BoundingBox(50.3, 30.3, 50.6, 30.8));
        var page = new PagedResult<PublicListingDto>([PublicDto()], 21, 3, 10);
        _listings
            .Setup(repository => repository.SearchAsync(expected, 3, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var result = await _sut.SearchAsync(filter);

        result.Value.ShouldBe(page);
    }

    [Fact]
    public async Task Search_ShouldPassNullArea_WhenFilterHasNoBoundingBox()
    {
        _listings
            .Setup(repository => repository.SearchAsync(
                It.IsAny<ListingSearchCriteria>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PublicListingDto>([], 0, 1, 20));

        await _sut.SearchAsync(new ListingFilterDto(Guests: 2));

        _listings.Verify(repository => repository.SearchAsync(
            new ListingSearchCriteria(2, null, null, null), 1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Map ----------

    [Fact]
    public async Task SearchInArea_ShouldRequestOneExtraRowWithSameCriteria_WhenRequestIsValid()
    {
        _listings
            .Setup(repository => repository.GetMarkersAsync(
                It.IsAny<ListingSearchCriteria>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _sut.SearchInAreaAsync(MapRequest(limit: 50));

        _listings.Verify(repository => repository.GetMarkersAsync(
            new ListingSearchCriteria(2, 500m, 3000m, new BoundingBox(50.3, 30.3, 50.6, 30.8)),
            51,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchInArea_ShouldReturnAllMarkersNotTruncated_WhenMatchesDoNotExceedLimit()
    {
        var markers = Markers(3);
        _listings
            .Setup(repository => repository.GetMarkersAsync(
                It.IsAny<ListingSearchCriteria>(), 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(markers);

        var result = await _sut.SearchInAreaAsync(MapRequest(limit: 3));

        result.Value.IsTruncated.ShouldBeFalse();
        result.Value.Markers.ShouldBe(markers);
    }

    [Fact]
    public async Task SearchInArea_ShouldCutToLimitAndMarkTruncated_WhenMoreListingsMatch()
    {
        var markers = Markers(4);
        _listings
            .Setup(repository => repository.GetMarkersAsync(
                It.IsAny<ListingSearchCriteria>(), 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(markers);

        var result = await _sut.SearchInAreaAsync(MapRequest(limit: 3));

        result.Value.IsTruncated.ShouldBeTrue();
        result.Value.Markers.ShouldBe(markers.Take(3));
    }

    [Fact]
    public async Task SearchInArea_ShouldReturnInvalidBoundingBoxAndNotQueryRepository_WhenBoxIsIncomplete()
    {
        var request = new MapSearchRequest(50.3, 30.3, null, 30.8);

        var result = await _sut.SearchInAreaAsync(request);

        result.Error.ShouldBe(ListingErrors.InvalidBoundingBox);
        _listings.Verify(repository => repository.GetMarkersAsync(
            It.IsAny<ListingSearchCriteria>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
