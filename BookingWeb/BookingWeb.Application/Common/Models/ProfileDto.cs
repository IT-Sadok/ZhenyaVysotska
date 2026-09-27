namespace BookingWeb.Application.Models;

public sealed record ProfileDto(
    Guid UserId, string FirstName, string LastName, string? Bio, string? AvatarUrl);