namespace BookingWeb.Application.Common.Settings;

public sealed class RefreshTokenSettings
{
    public const string SectionName = "RefreshTokens";

    public int ExpiryDays { get; init; } = 7;
}