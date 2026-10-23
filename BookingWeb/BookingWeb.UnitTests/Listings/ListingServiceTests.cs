using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Listings;
using BookingWeb.Application.Listings.Requests;
using BookingWeb.Domain.Models;
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