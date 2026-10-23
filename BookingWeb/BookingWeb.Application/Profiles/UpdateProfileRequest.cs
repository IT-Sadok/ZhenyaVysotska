namespace BookingWeb.Application.Profiles.Requests;

public sealed record UpdateProfileRequest(string FirstName, string LastName, string? Bio, string? AvatarUrl);