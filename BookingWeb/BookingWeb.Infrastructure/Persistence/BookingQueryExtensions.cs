using BookingWeb.Domain.Models;

namespace BookingWeb.Infrastructure.Persistence;

internal static class BookingQueryExtensions
{
    public static IQueryable<Booking> ThatBlock(
        this IQueryable<Booking> query, DateOnly checkIn, DateOnly checkOut)
    {
        return query.Where(b => 
            b.Status == BookingStatus.Confirmed
            && b.CheckIn < checkOut 
            && checkIn < b.CheckOut);
    }
}