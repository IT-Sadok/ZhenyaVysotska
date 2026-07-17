namespace Booking.Domain.Models;

public class Apartment
{
    private readonly Lock priceLock = new();
    private static int _nextId = 1;
    public int Id { get; set; }
    public Address Address { get; set; }
    public ApartmentType Type { get; set; }
    public decimal PricePerNight { get; set; }
    public int NumberOfRooms { get; set; }
    public bool IsAvailable { get; set; }

    public Apartment()
    {
        Id = _nextId++;
    }
    
    public void IncreasePriceSafely(decimal amount)
    {
        lock (priceLock)
        {
            PricePerNight += amount;
        }
    }
    
    public void IncreasePriceWithRaceCondition(decimal amount)
    {
        PricePerNight += amount;
    }
}