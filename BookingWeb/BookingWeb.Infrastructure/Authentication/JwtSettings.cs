namespace BookingWeb.Infrastructure.Authentication;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;
    public int ExpiryMinutes { get; init; } = 60;
    public int RefreshTokenExpiryDays { get; init; } = 7;
}