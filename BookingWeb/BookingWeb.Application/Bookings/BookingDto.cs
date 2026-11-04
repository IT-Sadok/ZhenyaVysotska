namespace BookingWeb.Application.Bookings;

public record BookingDto(
    Guid Id,
    Guid ListingId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Nights,
    int Guests,
    decimal TotalPrice,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string? CancelledBy);