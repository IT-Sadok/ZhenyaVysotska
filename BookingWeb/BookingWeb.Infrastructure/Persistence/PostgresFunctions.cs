using NpgsqlTypes;

namespace BookingWeb.Infrastructure.Persistence;

public static class PostgresFunctions
{
    public const string HalfOpen = "[)";
    
    public static NpgsqlRange<DateOnly> DateRange(DateOnly lower, DateOnly upper, string bounds) =>
        throw new InvalidOperationException($"{nameof(DateRange)} can only be used inside an EF Core query.");
}