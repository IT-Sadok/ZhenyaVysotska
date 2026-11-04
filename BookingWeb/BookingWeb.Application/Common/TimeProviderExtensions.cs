namespace BookingWeb.Application.Common;

public static class TimeProviderExtensions
{
    private static readonly TimeSpan WesternmostUtcOffset = TimeSpan.FromHours(-12);

    public static DateOnly GetUtcNowDateOnly(this TimeProvider timeProvider)
    {
        return DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
    }
    
    public static DateOnly GetLocalDate(this TimeProvider timeProvider, string timeZoneId)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var localNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone);
        return DateOnly.FromDateTime(localNow.DateTime);
    }
    
    public static DateOnly GetEarliestDateOnEarth(this TimeProvider timeProvider)
    {
        return DateOnly.FromDateTime(timeProvider.GetUtcNow().ToOffset(WesternmostUtcOffset).DateTime);
    }
}
