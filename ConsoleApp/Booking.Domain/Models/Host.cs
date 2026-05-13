namespace Booking.Domain.Models;

public class Host
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
  
    private readonly List<Apartment> _apartments = new List<Apartment>();
    
    public IReadOnlyCollection<Apartment> Apartments => _apartments.AsReadOnly();

    public void AddApartment(Apartment apartment)
    {
        if(apartment == null) 
            throw new ArgumentNullException(nameof(apartment));
        if(_apartments.Contains(apartment))
            throw new InvalidOperationException("The apartment already exists.");
        _apartments.Add(apartment);
    }
}