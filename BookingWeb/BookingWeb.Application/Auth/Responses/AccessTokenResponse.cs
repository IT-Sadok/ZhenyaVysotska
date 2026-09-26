namespace BookingWeb.Application.Auth.Responses;

public sealed record AccessTokenResponse(string AccessToken, DateTime ExpiresAtUtc);