namespace Booking.Domain.Models;

public class Apartment
{
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
}