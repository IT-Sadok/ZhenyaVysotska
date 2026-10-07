namespace BookingWeb.Application.Auth.Responses;

public record AuthResponse(
    string AccessToken, DateTime ExpiresAtUtc, 
    string RefreshToken, DateTimeOffset RefreshTokenExpiresAtUtc);