namespace BookingWeb.Application.Bookings;

public sealed class BookingOverlapException : Exception
{
    public BookingOverlapException(Exception innerException)
        : base("The booking overlaps an existing confirmed booking.", innerException)
    {
        
    }
}
