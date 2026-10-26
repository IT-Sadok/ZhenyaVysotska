using BookingWeb.Application.Bookings.Requests;
using BookingWeb.Application.Common;
using FluentValidation;

namespace BookingWeb.Application.Bookings.Validators;

public sealed class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    
    public CreateBookingRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x=> x.ListingId).NotEmpty();
        
        RuleFor(x => x.CheckIn)
            .Must(checkIn => checkIn >= timeProvider.GetUtcToday())
            .WithMessage("CheckIn can't be in the past.")
            .Must(checkIn => checkIn <= timeProvider.GetUtcToday().AddDays(BookingRules.MaxDaysAhead))
            .WithMessage($"CheckIn can be at most {BookingRules.MaxDaysAhead} days ahead.");

        RuleFor(x => x.CheckOut)
            .GreaterThan(x => x.CheckIn)
            .WithMessage("CheckOut must be after CheckIn.")
            .Must((request, checkOut) => checkOut.DayNumber - request.CheckIn.DayNumber <= BookingRules.MaxNights)
            .WithMessage($"A stay can be at most {BookingRules.MaxNights} nights.");

        RuleFor(x => x.Guests).GreaterThanOrEqualTo(1);
    }
}