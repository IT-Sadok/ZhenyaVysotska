using BookingWeb.Application.Bookings;
using BookingWeb.Application.Common.Interfaces;
using BookingWeb.Application.Models;
using BookingWeb.Domain.Entities;
using BookingWeb.Domain.ValueObjects;
using BookingWeb.UnitTests.TestDoubles;
using Moq;
using Shouldly;

namespace BookingWeb.UnitTests.Bookings;

public sealed class BookingServiceHostTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    private readonly Guid _hostId = Guid.NewGuid();
    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IListingRepository> _listings = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly BookingService _sut;

    public BookingServiceHostTests()
    {
        _sut = new BookingService(_bookings.Object, _listings.Object, _unitOfWork.Object, new FixedTimeProvider(Now));
    }

    private Booking SetupBookingOnHostListing(Guid? listingId = null)
    {
        var listing = Listing.Create(
            _hostId, "Loft", null, "Kyiv", "Kyiv", "Khreshchatyk 1",
            location: new GeoPoint(50.4501, 30.5234), publicLocation: new GeoPoint(50.4530, 30.5260),
            1000m, 4, Now);
        _listings
            .Setup(repository => repository.GetByIdAsync(listing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listing);

        var booking = Booking.Create(
            listingId ?? listing.Id, Guid.NewGuid(), new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 12), 2, 1000m, Now);
        _bookings
            .Setup(repository => repository.GetByIdForHostAsync(booking.Id, _hostId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        return booking;
    }

    [Fact]
    public async Task CancelByHost_ShouldCancelAsHost_WhenBookingIsOnHostListing()
    {
        var booking = SetupBookingOnHostListing();

        var result = await _sut.CancelByHostAsync(_hostId, booking.ListingId, booking.Id);

        result.IsSuccess.ShouldBeTrue();
        booking.Status.ShouldBe(BookingStatus.Cancelled);
        booking.CancelledBy.ShouldBe(BookingCanceller.Host);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelByHost_ShouldReturnNotFound_WhenBookingBelongsToAnotherListing()
    {
        var booking = SetupBookingOnHostListing(Guid.NewGuid());

        var result = await _sut.CancelByHostAsync(_hostId, Guid.NewGuid(), booking.Id);

        result.Error.ShouldBe(BookingErrors.NotFound);
        booking.Status.ShouldBe(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task CancelByHost_ShouldReturnNotFound_WhenBookingIsNotOnHostListing()
    {
        var result = await _sut.CancelByHostAsync(_hostId, Guid.NewGuid(), Guid.NewGuid());

        result.Error.ShouldBe(BookingErrors.NotFound);
    }

    [Fact]
    public async Task GetForHostListing_ShouldReturnListingNotFound_WhenListingBelongsToOtherHost()
    {
        var result = await _sut.GetForHostListingAsync(_hostId, Guid.NewGuid(), new PageRequest());

        result.Error.ShouldBe(BookingErrors.ListingNotFound);
        _bookings.Verify(
            repository => repository.GetForListingAsync(It.IsAny<Guid>(), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
