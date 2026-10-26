using BookingWeb.Domain.Models;
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
    public void CanBeCancelledAt_ShouldReturnTrue_WhenItIsTheDayBeforeCheckIn()
    {
        var booking = CreateBooking();

        booking.CanBeCancelledAt(At(CheckIn.AddDays(-1), hour: 23)).ShouldBeTrue();
    }
    
    [Fact]
    public void CanBeCancelledAt_ShouldReturnFalse_WhenItIsTheCheckInDay()
    {
        var booking = CreateBooking();

        booking.CanBeCancelledAt(At(CheckIn, hour: 0)).ShouldBeFalse();
    }

    [Fact]
    public void Cancel_ShouldSetCancelledStatusAndTime_WhenCancelledBeforeCheckIn()
    {
        var booking = CreateBooking();
        var now = At(CheckIn.AddDays(-5));

        booking.Cancel(now, BookingCanceller.Guest);

        booking.Status.ShouldBe(BookingStatus.Cancelled);
        booking.CancelledAtUtc.ShouldBe(now);
        booking.CancelledBy.ShouldBe(BookingCanceller.Guest);
    }
    
    [Fact]
    public void Cancel_ShouldThrow_WhenBookingIsAlreadyCancelled()
    {
        var booking = CreateBooking();
        booking.Cancel(At(CheckIn.AddDays(-5)), BookingCanceller.Guest);

        Should.Throw<InvalidOperationException>(() => booking.Cancel(At(CheckIn.AddDays(-4)), BookingCanceller.Guest));
    }
}