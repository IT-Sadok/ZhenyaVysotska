namespace BookingWeb.Application.Common;

public static class TimeProviderExtensions
{
    /// <summary>The platform works only in Ukraine, so booking dates are Kyiv calendar dates.</summary>
    public const string PlatformTimeZoneId = "Europe/Kyiv";

    // Resolved once: the lookup reads the OS time zone database.
    private static readonly TimeZoneInfo PlatformTimeZone = TimeZoneInfo.FindSystemTimeZoneById(PlatformTimeZoneId);

    /// <summary>Today's date in Kyiv - the "today" of all booking rules.</summary>
    public static DateOnly GetPlatformToday(this TimeProvider timeProvider)
    {
        var kyivNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), PlatformTimeZone);
        return DateOnly.FromDateTime(kyivNow.DateTime);
    }
}
