using BookingWeb.Domain.Entities;
using Shouldly;

namespace BookingWeb.UnitTests.Domain;

public sealed class BookingTests
{
    private static readonly DateOnly CheckIn = new(2026, 5, 10);
    private static readonly DateOnly CheckOut = new(2026, 5, 13);
    private static readonly DateTimeOffset CreatedAt = new(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);

    private static Booking CreateBooking()
    {
        return Booking.Create(Guid.NewGuid(), Guid.NewGuid(), CheckIn, CheckOut, 2, 1000m, CreatedAt);
    }

    private static DateTimeOffset At(DateOnly day, int hour = 12)
    {
        return new DateTimeOffset(day.ToDateTime(new TimeOnly(hour, 0)), TimeSpan.Zero);
    }

    [Fact]
    public void Create_ShouldCreateConfirmedBookingWithTotalPrice_WhenArgumentsAreValid()
    {
        var booking = CreateBooking();

        booking.Status.ShouldBe(BookingStatus.Confirmed);
        booking.Nights.ShouldBe(3);
        booking.TotalPrice.ShouldBe(3000m);
        booking.CancelledAtUtc.ShouldBeNull();
    }
    
    [Fact]
    public void Create_ShouldThrow_WhenCheckOutEqualsCheckIn()
    {
        Should.Throw<ArgumentException>(() =>
            Booking.Create(Guid.NewGuid(), Guid.NewGuid(), CheckIn, CheckIn, 2, 1000m, CreatedAt));
    }
    
    [Fact]
    public void Create_ShouldThrow_WhenGuestsIsZero()
    {
        Should.Throw<ArgumentException>(() =>
            Booking.Create(Guid.NewGuid(), Guid.NewGuid(), CheckIn, CheckOut, 0, 1000m, CreatedAt));
    }

    [Fact]
    public void CanBeCancelledOn_ShouldReturnTrue_WhenTodayIsTheDayBeforeCheckIn()
    {
        var booking = CreateBooking();

        booking.CanBeCancelledOn(CheckIn.AddDays(-1)).ShouldBeTrue();
    }

    [Fact]
    public void CanBeCancelledOn_ShouldReturnFalse_WhenTodayIsTheCheckInDay()
    {
        var booking = CreateBooking();

        booking.CanBeCancelledOn(CheckIn).ShouldBeFalse();
    }

    [Fact]
    public void Cancel_ShouldThrowAndKeepConfirmed_WhenTodayIsTheCheckInDay()
    {
        var booking = CreateBooking();
        
        Should.Throw<InvalidOperationException>(() =>
            booking.Cancel(At(CheckIn.AddDays(-1), hour: 20), CheckIn, BookingCanceller.Guest));

        booking.Status.ShouldBe(BookingStatus.Confirmed);
    }

    [Fact]
    public void Cancel_ShouldSetCancelledStatusAndTime_WhenCancelledBeforeCheckIn()
    {
        var booking = CreateBooking();
        var now = At(CheckIn.AddDays(-5));

        booking.Cancel(now, CheckIn.AddDays(-5), BookingCanceller.Guest);

        booking.Status.ShouldBe(BookingStatus.Cancelled);
        booking.CancelledAtUtc.ShouldBe(now);
        booking.CancelledBy.ShouldBe(BookingCanceller.Guest);
    }
    
    [Fact]
    public void Cancel_ShouldThrow_WhenBookingIsAlreadyCancelled()
    {
        var booking = CreateBooking();
        booking.Cancel(At(CheckIn.AddDays(-5)), CheckIn.AddDays(-5), BookingCanceller.Guest);

        Should.Throw<InvalidOperationException>(() => booking.Cancel(At(CheckIn.AddDays(-4)), CheckIn.AddDays(-4), BookingCanceller.Guest));
    }

    [Theory]
    [InlineData(10, 13)] 
    [InlineData(8, 11)]  
    [InlineData(12, 15)] 
    [InlineData(11, 12)] 
    [InlineData(5, 20)]  
    public void Blocks_ShouldReturnTrue_WhenRequestedStayOverlapsConfirmedBooking(int checkInDay, int checkOutDay)
    {
        var booking = CreateBooking();

        Blocks(booking, May(checkInDay), May(checkOutDay)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(8, 10)] 
    [InlineData(13, 15)] 
    [InlineData(1, 5)]   
    [InlineData(20, 25)] 
    public void Blocks_ShouldReturnFalse_WhenRequestedStayDoesNotOverlap(int checkInDay, int checkOutDay)
    {
        var booking = CreateBooking();

        Blocks(booking, May(checkInDay), May(checkOutDay)).ShouldBeFalse();
    }

    [Fact]
    public void Blocks_ShouldReturnFalse_WhenOverlappingBookingIsCancelled()
    {
        var booking = CreateBooking();
        booking.Cancel(At(CheckIn.AddDays(-5)), CheckIn.AddDays(-5), BookingCanceller.Guest);

        Blocks(booking, CheckIn, CheckOut).ShouldBeFalse();
    }

    [Theory]
    [InlineData(1)]  
    [InlineData(10)] 
    [InlineData(12)] 
    public void IsUpcoming_ShouldReturnTrue_WhenConfirmedBookingHasNotEnded(int todayDay)
    {
        var booking = CreateBooking();

        IsUpcoming(booking, May(todayDay)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(13)] 
    [InlineData(20)] 
    public void IsUpcoming_ShouldReturnFalse_WhenBookingHasEnded(int todayDay)
    {
        var booking = CreateBooking();

        IsUpcoming(booking, May(todayDay)).ShouldBeFalse();
    }

    [Fact]
    public void IsUpcoming_ShouldReturnFalse_WhenBookingIsCancelled()
    {
        var booking = CreateBooking();
        booking.Cancel(At(CheckIn.AddDays(-5)), CheckIn.AddDays(-5), BookingCanceller.Guest);

        IsUpcoming(booking, CheckIn.AddDays(-1)).ShouldBeFalse();
    }

    private static DateOnly May(int day) => new(2026, 5, day);
    
    private static bool Blocks(Booking booking, DateOnly checkIn, DateOnly checkOut) =>
        Booking.Blocks(checkIn, checkOut).Compile()(booking);

    private static bool IsUpcoming(Booking booking, DateOnly today) =>
        Booking.IsUpcoming(today).Compile()(booking);
}