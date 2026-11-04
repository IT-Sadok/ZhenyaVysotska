namespace BookingWeb.Application.Auth;

public sealed record UserDto(
    Guid Id, string Email, IEnumerable<string> Roles);