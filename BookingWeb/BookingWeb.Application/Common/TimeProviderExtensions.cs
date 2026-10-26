namespace BookingWeb.Application.Common;

public static class TimeProviderExtensions
{
    public static DateOnly GetUtcToday(this TimeProvider timeProvider)
    {
        return DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
    }
}