using BookingWeb.Application.Bookings;
using BookingWeb.Application.Bookings.Requests;
using BookingWeb.Application.Common.Interfaces;
using BookingWeb.Application.Results;
using BookingWeb.Domain.Entities;
using BookingWeb.Domain.ValueObjects;
using BookingWeb.UnitTests.TestDoubles;
using Moq;
using Shouldly;

namespace BookingWeb.UnitTests.Bookings;

public sealed class BookingServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly CheckIn = new(2026, 3, 10);
    private static readonly DateOnly CheckOut = new(2026, 3, 13);

    private readonly Guid _guestId = Guid.NewGuid();
    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IListingRepository> _listings = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly BookingService _sut;
    
    public BookingServiceTests()
    {
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _sut = CreateSut(Now);
    }

    private BookingService CreateSut(DateTimeOffset utcNow)
    {
        return new BookingService(
            _bookings.Object,
            _listings.Object,
            _unitOfWork.Object,
            new FixedTimeProvider(utcNow));
    }
    
    private static Listing CreateListing(Guid? hostId = null, int maxGuests = 4, string timeZoneId = "Europe/Kyiv")
    {
        return Listing.Create(
            hostId ?? Guid.NewGuid(), "Loft", null, "UA", "Kyiv", "Kyiv", "Khreshchatyk 1",
            location: new GeoPoint(50.4501, 30.5234), publicLocation: new GeoPoint(50.4530, 30.5260),
            timeZoneId, 1000m, maxGuests, Now);
    }

    private void SetupListing(Listing listing)
    {
        _listings
            .Setup(repository => repository.GetByIdAsync(listing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listing);
    }

    private static CreateBookingRequest CreateRequest(Listing listing, int guests = 2)
    {
        return new CreateBookingRequest(listing.Id, CheckIn, CheckOut, guests);
    }

    private Booking SetupBookingOfGuest(Listing listing)
    {
        SetupListing(listing);
        var booking = Booking.Create(listing.Id, _guestId, CheckIn, CheckOut, 2, 1000m, Now);
        _bookings
            .Setup(repository => repository.GetByIdForGuestAsync(booking.Id, _guestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        return booking;
    }

    [Fact]
    public async Task Create_ShouldCreateConfirmedBookingWithSnapshotPrice_WhenDatesAreFree()
    {
        var listing = CreateListing();
        SetupListing(listing);

        var result = await _sut.CreateAsync(_guestId, CreateRequest(listing));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(nameof(BookingStatus.Confirmed));
        result.Value.TotalPrice.ShouldBe(3000m);
        _bookings.Verify(
            repository => repository.Add(It.Is<Booking>(booking =>
                booking.GuestId == _guestId && booking.ListingId == listing.Id)),
            Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ShouldReturnListingNotFound_WhenListingIsInactive()
    {
        var listing = CreateListing();
        listing.Deactivate();
        SetupListing(listing);

        var result = await _sut.CreateAsync(_guestId, CreateRequest(listing));

        result.Error.ShouldBe(BookingErrors.ListingNotFound);
        _bookings.Verify(repository => repository.Add(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task Create_ShouldReturnOwnListingError_WhenHostBooksOwnListing()
    {
        var listing = CreateListing(hostId: _guestId);
        SetupListing(listing);

        var result = await _sut.CreateAsync(_guestId, CreateRequest(listing));

        result.Error.ShouldBe(BookingErrors.OwnListing);
    }

    [Fact]
    public async Task Create_ShouldReturnTooManyGuests_WhenGuestsExceedMaxGuests()
    {
        var listing = CreateListing(maxGuests: 2);
        SetupListing(listing);

        var result = await _sut.CreateAsync(_guestId, CreateRequest(listing, guests: 3));

        result.Error.Code.ShouldBe("Booking.TooManyGuests");
        result.Error.Type.ShouldBe(ErrorType.Failure);
        _bookings.Verify(repository => repository.Add(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task Create_ShouldReturnDatesUnavailableAndNotSave_WhenOverlapExists()
    {
        var listing = CreateListing();
        SetupListing(listing);
        _bookings
            .Setup(repository => repository.HasOverlapAsync(listing.Id, CheckIn, CheckOut, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.CreateAsync(_guestId, CreateRequest(listing));

        result.Error.ShouldBe(BookingErrors.DatesUnavailable);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_ShouldReturnDatesUnavailable_WhenSaveReportsOverlap()
    {
        var listing = CreateListing();
        SetupListing(listing);
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(BookingErrors.DatesUnavailable));

        var result = await _sut.CreateAsync(_guestId, CreateRequest(listing));

        result.Error.ShouldBe(BookingErrors.DatesUnavailable);
    }
    
    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenBookingBelongsToOtherGuest()
    {
        var result = await _sut.GetByIdAsync(_guestId, Guid.NewGuid());

        result.Error.ShouldBe(BookingErrors.NotFound);
    }

    // Tokyo is UTC+9: at 16:00 UTC on Mar 9 it is already 01:00 on Mar 10 there.
    private static readonly DateTimeOffset TokyoJustAfterMidnightOnCheckInDay = new(2026, 3, 9, 16, 0, 0, TimeSpan.Zero);

    // Honolulu is UTC-10: at 06:00 UTC on Mar 10 it is still 20:00 on Mar 9 there.
    private static readonly DateTimeOffset HonoluluEveningBeforeCheckInDay = new(2026, 3, 10, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_ShouldReturnCheckInInPast_WhenCheckInIsBeforeTodayInListingTimeZone()
    {
        var listing = CreateListing(timeZoneId: "Asia/Tokyo");
        SetupListing(listing);
        var sut = CreateSut(TokyoJustAfterMidnightOnCheckInDay);
        var yesterdayInTokyo = CheckIn.AddDays(-1); // still "today" in UTC

        var result = await sut.CreateAsync(
            _guestId, new CreateBookingRequest(listing.Id, yesterdayInTokyo, CheckOut, 2));

        result.Error.ShouldBe(BookingErrors.CheckInInPast);
        _bookings.Verify(repository => repository.Add(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task Create_ShouldCreateBooking_WhenCheckInIsTodayInListingTimeZoneButYesterdayInUtc()
    {
        var listing = CreateListing(timeZoneId: "Pacific/Honolulu");
        SetupListing(listing);
        var sut = CreateSut(HonoluluEveningBeforeCheckInDay);
        var todayInHonolulu = CheckIn.AddDays(-1);

        var result = await sut.CreateAsync(
            _guestId, new CreateBookingRequest(listing.Id, todayInHonolulu, CheckOut, 2));

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Cancel_ShouldReturnCannotBeCancelled_WhenCheckInDayHasStartedInListingTimeZone()
    {
        var booking = SetupBookingOfGuest(CreateListing(timeZoneId: "Asia/Tokyo"));
        var sut = CreateSut(TokyoJustAfterMidnightOnCheckInDay);

        var result = await sut.CancelAsync(_guestId, booking.Id);

        result.Error.ShouldBe(BookingErrors.CannotBeCancelled);
        booking.Status.ShouldBe(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task Cancel_ShouldCancel_WhenCheckInDayHasNotStartedInListingTimeZone()
    {
        var booking = SetupBookingOfGuest(CreateListing(timeZoneId: "Pacific/Honolulu"));
        var sut = CreateSut(HonoluluEveningBeforeCheckInDay);

        var result = await sut.CancelAsync(_guestId, booking.Id);

        result.IsSuccess.ShouldBeTrue();
        booking.Status.ShouldBe(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_ShouldCancelAndSave_WhenCheckInIsInTheFuture()
    {
        var booking = SetupBookingOfGuest(CreateListing());

        var result = await _sut.CancelAsync(_guestId, booking.Id);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(nameof(BookingStatus.Cancelled));
        result.Value.CancelledBy.ShouldBe(nameof(BookingCanceller.Guest));
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_ShouldReturnCannotBeCancelled_WhenBookingIsAlreadyCancelled()
    {
        var booking = SetupBookingOfGuest(CreateListing());
        booking.Cancel(Now, DateOnly.FromDateTime(Now.UtcDateTime), BookingCanceller.Guest);

        var result = await _sut.CancelAsync(_guestId, booking.Id);

        result.Error.ShouldBe(BookingErrors.CannotBeCancelled);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_ShouldReturnNotFound_WhenBookingBelongsToOtherGuest()
    {
        var result = await _sut.CancelAsync(_guestId, Guid.NewGuid());

        result.Error.ShouldBe(BookingErrors.NotFound);
    }
}