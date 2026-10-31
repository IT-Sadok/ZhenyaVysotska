using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Listings;
using BookingWeb.Application.Listings.Requests;
using BookingWeb.Application.Models;
using BookingWeb.Domain.Entities;
using BookingWeb.UnitTests.TestDoubles;
using Moq;
using Shouldly;

namespace BookingWeb.UnitTests.Listings;

public sealed class ListingServiceTests
{
    private static readonly DateTimeOffset Now = new (2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
    
    private readonly Guid _hostId = Guid.NewGuid();
    private readonly Mock<IListingRepository> _listings = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ListingService _sut;

    public ListingServiceTests()
    {
        _sut = new ListingService(_listings.Object, _unitOfWork.Object, new FixedTimeProvider(Now));
    }

    private Listing CreateOwnListing()
    {
        var listing = Listing.Create(_hostId, "Loft", null, "Kyiv", "Khreshchatyk 1", 1000m, 2, Now);
        _listings
            .Setup(repository => repository.
                GetByIdForHostAsync(listing.Id, _hostId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listing);
        return listing;
    }

    private static ListingRequest CreateRequest()
    {
        return new ListingRequest("Sea view", "Near the beach", "Odesa", "Prymorska 1", 2000m, 4);
    }

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
    public async Task GetById_ShouldReturnNotFound_WhenListingIsInactive()
    {
        var listing = Listing.Create(_hostId, "Loft", null, "Kyiv", "Khreshchatyk 1", 1000m, 2, Now);
        listing.Deactivate();
        _listings
            .Setup(repository => repository.GetByIdAsync(listing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listing);

        var result = await _sut.GetByIdAsync(listing.Id);

        result.Error.ShouldBe(ListingErrors.NotFound);
    }

    [Fact]
    public async Task GetMine_ShouldReturnEmptyList_WhenHostHasNoListings()
    {
        _listings
            .Setup(repository => repository.GetByHostAsync(_hostId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.GetMineAsync(_hostId);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetMine_ShouldMapEveryListing_WhenHostHasListings()
    {
        var first = Listing.Create(_hostId, "Loft", null, "Kyiv", "Khreshchatyk 1", 1000m, 2, Now);
        var second = Listing.Create(_hostId, "Villa", "Pool", "Odesa", "Prymorska 1", 3000m, 6, Now);
        _listings
            .Setup(repository => repository.GetByHostAsync(_hostId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([first, second]);

        var result = await _sut.GetMineAsync(_hostId);

        result.Value.Select(dto => dto.Id).ShouldBe([first.Id, second.Id]);
        result.Value.Select(dto => dto.Title).ShouldBe(["Loft", "Villa"]);
    }

    [Fact]
    public async Task Search_ShouldReturnEmptyItemsAndKeepPaging_WhenNothingMatches()
    {
        var filter = new ListingFilterDto("Lviv", null, Page: 3, PageSize: 10);
        _listings
            .Setup(repository => repository.SearchAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Listing>([], 0, 3, 10));

        var result = await _sut.SearchAsync(filter);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldNotBeNull();
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
        result.Value.Page.ShouldBe(3);
        result.Value.PageSize.ShouldBe(10);
    }

    [Fact]
    public async Task Search_ShouldMapItemsAndKeepTotalCount_WhenListingsMatch()
    {
        var filter = new ListingFilterDto("Kyiv", null, Page: 1, PageSize: 1);
        var listing = Listing.Create(_hostId, "Loft", null, "Kyiv", "Khreshchatyk 1", 1000m, 2, Now);
        _listings
            .Setup(repository => repository.SearchAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Listing>([listing], 5, 1, 1));

        var result = await _sut.SearchAsync(filter);

        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items[0].Id.ShouldBe(listing.Id);
        result.Value.TotalCount.ShouldBe(5);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenListingBelongsToOtherHost()
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
        listing.City.ShouldBe("Odesa");
        listing.PricePerNight.ShouldBe(2000m);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
}