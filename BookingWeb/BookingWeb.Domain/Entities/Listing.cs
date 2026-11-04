using Ardalis.GuardClauses;
using BookingWeb.Domain.ValueObjects;

namespace BookingWeb.Domain.Entities;

public sealed class Listing
{
    public const int CountryCodeLength = 2;

    public Guid Id { get; private set; }
    public Guid HostId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string CountryCode { get; private set; } = null!;
    public string? Region { get; private set; }
    public string City { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public GeoPoint Location { get; private set; } = null!;
    public GeoPoint PublicLocation { get; private set; } = null!;
    public decimal PricePerNight { get; private set; }
    public int MaxGuests { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Listing() { }

    public static Listing Create(
        Guid hostId,
        string title,
        string? description,
        string countryCode,
        string? region,
        string city,
        string address,
        GeoPoint location,
        GeoPoint publicLocation,
        decimal pricePerNight,
        int maxGuests,
        DateTimeOffset createdAtUtc)
    {
        Guard.Against.Default(hostId);

        var listing = new Listing
        {
            Id = Guid.CreateVersion7(createdAtUtc),
            HostId = hostId,
            IsActive = true,
            CreatedAtUtc = createdAtUtc
        };

        listing.Update(title, description, countryCode, region, city, address, pricePerNight, maxGuests);
        listing.SetLocation(location, publicLocation);
        return listing;
    }

    public void Update(
        string title,
        string? description,
        string countryCode,
        string? region,
        string city,
        string address,
        decimal pricePerNight,
        int maxGuests)
    {
        Guard.Against.NullOrWhiteSpace(title);
        Guard.Against.NullOrWhiteSpace(countryCode);
        Guard.Against.NullOrWhiteSpace(city);
        Guard.Against.NullOrWhiteSpace(address);
        Guard.Against.NegativeOrZero(pricePerNight);
        Guard.Against.NegativeOrZero(maxGuests);
        
        var normalizedCountryCode = countryCode.Trim().ToUpperInvariant();
        Guard.Against.InvalidFormat(normalizedCountryCode, nameof(countryCode), "^[A-Z]{2}$",
            "Country code must be two Latin letters (ISO 3166-1 alpha-2).");

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        CountryCode = normalizedCountryCode;
        Region = string.IsNullOrWhiteSpace(region) ? null : region.Trim();
        City = city.Trim();
        Address = address.Trim();
        PricePerNight = pricePerNight;
        MaxGuests = maxGuests;
    }
    
    public void SetLocation(GeoPoint location, GeoPoint publicLocation)
    {
        Guard.Against.Null(location);
        Guard.Against.Null(publicLocation);
        if (publicLocation == location)
        {
            throw new ArgumentException(
                "Public location must differ from the exact location.", nameof(publicLocation));
        }

        Location = location;
        PublicLocation = publicLocation;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
