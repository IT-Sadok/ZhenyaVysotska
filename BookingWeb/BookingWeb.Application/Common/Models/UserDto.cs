namespace BookingWeb.Application.Models;

public sealed record UserDto(
    Guid Id, string Email, IEnumerable<string> Roles);