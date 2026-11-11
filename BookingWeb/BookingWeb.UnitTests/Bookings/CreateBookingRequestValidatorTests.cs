using BookingWeb.Application.Bookings;
using BookingWeb.Application.Bookings.Requests;
using BookingWeb.Application.Bookings.Validators;
using BookingWeb.UnitTests.TestDoubles;
using FluentValidation.TestHelper;

namespace BookingWeb.UnitTests.Bookings;

public sealed class CreateBookingRequestValidatorTests
{
    private static readonly DateOnly Today = new(2026, 3, 1);

    private readonly CreateBookingRequestValidator _validator =
        new(new FixedTimeProvider(new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero)));

    private static CreateBookingRequest CreateValidRequest()
    {
        return new CreateBookingRequest(Guid.NewGuid(), Today.AddDays(10), Today.AddDays(13), 2);
    }

    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        _validator.TestValidate(CreateValidRequest()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldPass_WhenCheckInIsToday()
    {
        var request = CreateValidRequest() with { CheckIn = Today, CheckOut = Today.AddDays(1) };

        _validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenCheckInIsYesterdayInKyiv()
    {
        var request = CreateValidRequest() with { CheckIn = Today.AddDays(-1) };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.CheckIn);
    }

    [Fact]
    public void Validate_ShouldFail_WhenCheckInIsTodayInUtcButYesterdayInKyiv()
    {
        // 22:30 UTC on Feb 28 is already 00:30 on Mar 1 in Kyiv (UTC+2 in winter).
        var validator = new CreateBookingRequestValidator(new FixedTimeProvider(new DateTimeOffset(2026, 2, 28, 22, 30, 0, TimeSpan.Zero)));
        var request = CreateValidRequest() with { CheckIn = new DateOnly(2026, 2, 28) };

        validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.CheckIn);
    }

    [Fact]
    public void Validate_ShouldFail_WhenCheckOutEqualsCheckIn()
    {
        var request = CreateValidRequest() with { CheckOut = Today.AddDays(10) };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.CheckOut);
    }

    [Fact]
    public void Validate_ShouldFail_WhenStayIsLongerThanMaxNights()
    {
        var request = CreateValidRequest() with { CheckOut = Today.AddDays(10 + BookingRules.MaxNights + 1) };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.CheckOut);
    }

    [Fact]
    public void Validate_ShouldFail_WhenCheckInIsBeyondBookingHorizon()
    {
        var checkIn = Today.AddDays(BookingRules.MaxDaysAhead + 1);
        var request = CreateValidRequest() with { CheckIn = checkIn, CheckOut = checkIn.AddDays(1) };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.CheckIn);
    }

    [Fact]
    public void Validate_ShouldFail_WhenListingIdIsEmpty()
    {
        var request = CreateValidRequest() with { ListingId = Guid.Empty };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.ListingId);
    }
}
