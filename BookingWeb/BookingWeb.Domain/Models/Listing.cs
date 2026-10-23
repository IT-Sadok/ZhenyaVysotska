namespace BookingWeb.Domain.Models;

public sealed class Listing
{
    public Guid Id { get; private set; }
    public Guid HostId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string City { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public decimal PricePerNight { get; private set; }
    public int MaxGuests { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Listing() { }

    public static Listing Create(
        Guid hostId,
        string title,
        string? description,
        string city,
        string address,
        decimal pricePerNight,
        int maxGuests,
        DateTimeOffset createdAtUtc)
    {
        Guard.ThrowIfEmpty(hostId);

        var listing = new Listing
        {
            Id = Guid.NewGuid(),
            HostId = hostId,
            IsActive = true,
            CreatedAtUtc = createdAtUtc
        };

        listing.Update(title, description, city, address, pricePerNight, maxGuests);
        return listing;
    }

    public void Update(
        string title,
        string? description,
        string city,
        string address,
        decimal pricePerNight,
        int maxGuests)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pricePerNight);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxGuests, 1);
        
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        City = city.Trim();
        Address = address.Trim();
        PricePerNight = pricePerNight;
        MaxGuests = maxGuests;
    }
    
    public void Deactivate()
    {
        IsActive = false;
    }
}