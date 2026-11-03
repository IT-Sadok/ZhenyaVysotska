namespace BookingWeb.Application.Common;

public static class TimeProviderExtensions
{
    public static DateOnly GetUtcNowDateOnly(this TimeProvider timeProvider)
    {
        return DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
    }
}