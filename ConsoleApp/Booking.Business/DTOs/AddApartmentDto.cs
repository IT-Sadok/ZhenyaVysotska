using Booking.Domain.Models;
namespace Booking.Business.DTOs;

public class AddApartmentDto
{
    public List<int> HostIds { get; set; }
    public Address Address { get; set; }
    public ApartmentType Type { get; set; }
    public decimal PricePerNight { get; set; }
    public int NumberOfRooms { get; set; }
    public bool IsAvailable { get; set; }
}