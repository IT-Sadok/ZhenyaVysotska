using Booking.Business.DTOs;
using Booking.Domain.Interfaces;
using Booking.Domain.Models;

namespace Booking.Business.Services;

public class HostService
{
    private readonly IHostRepository _repository;
    public HostService(IHostRepository repository)
    {
        _repository = repository;
    }

    public (bool IsSuccess, string Message) CreateHost(AddHostDto dto)
    {
        if(string.IsNullOrWhiteSpace(dto.FirstName)) return (false, "First name is required");
        if(string.IsNullOrWhiteSpace(dto.LastName)) return (false, "Last name is required");
        if(string.IsNullOrWhiteSpace(dto.Email) || !dto.Email.Contains("@")) return (false, "Email is required");
        if(string.IsNullOrWhiteSpace(dto.PhoneNumber) || dto.PhoneNumber.Length < 10) return (false, "Phone number must be at least 10 characters long");
        
        var newHost = new Host
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber
        };
        
        _repository.Add(newHost);
        return (true, $"Host with id {newHost.Id} has been created");
    }
    
    public Host GetHostById(int id)
    {
        return _repository.GetById(id);
    }

    public List<Host> GetAllHosts()
    {
        return _repository.GetAll();
    }

    public (bool IsSuccess, string Message) AddApartmentToHost(AddApartmentDto dto)
    {
        if(dto.PricePerNight <= 0)  return (false, "Price per night must be greater than 0");
        if(dto.NumberOfRooms <= 0) return (false, "Number of rooms must be greater than 0");
        
        if (dto.Address == null) return (false, "Address information is missing");
        if(string.IsNullOrWhiteSpace(dto.Address.City)) return (false, "Address city must be set");
        if (string.IsNullOrWhiteSpace(dto.Address.Street)) return (false, "Address street must be set");
        if (string.IsNullOrWhiteSpace(dto.Address.HouseNumber)) return (false, "Number of house must be set");

        var hostsToUpdate = new List<Host>();
        foreach (int hostId in dto.HostIds)
        {
            var host = _repository.GetById(hostId);
            if (host == null)
            {
                return (false, $"Host with id {hostId} not found. Operation aborted.");
            }
            hostsToUpdate.Add(host);
        }
        var newApartment = new Apartment
        {
            Address = dto.Address,
            Type = dto.Type,
            PricePerNight = dto.PricePerNight,
            NumberOfRooms = dto.NumberOfRooms,
            IsAvailable = dto.IsAvailable
        };
        
        foreach (var host in hostsToUpdate)
        {
            host.AddApartment(newApartment);
            _repository.Update(host);
        }
        return (true, $"Apartment with id {newApartment.Id} has been added");
    }

    public (bool IsSuccess, string Message) UpdateHost(UpdateHostDto dto)
    {
        var host = _repository.GetById(dto.Id);
        if (host == null) return (false, $"Host with id {dto.Id} not found");
        if(string.IsNullOrWhiteSpace(dto.FirstName)) return (false, "First name is required");
        if(string.IsNullOrWhiteSpace(dto.LastName)) return (false, "Last name is required");
        
        host.FirstName = dto.FirstName;
        host.LastName = dto.LastName;
        host.Email = dto.Email;
        host.PhoneNumber = dto.PhoneNumber;
        
        _repository.Update(host);
        return (true, $"Host with id {host.Id} has been updated");
    }

    public (bool IsSuccess, string Message) DeleteHost(int id)
    {
        var host = _repository.GetById(id);
        if (host == null)
        {
            return (false, $"User with id {id} not found");
        }

        _repository.Delete(id);
        return (true, $"User with id {id} has been deleted");
    }

    public (bool IsSuccess, string Message) SaveToFile()
    {
        try
        {
            _repository.Save();
            return (true, "Data saved successfully.");
        }
        catch (Exception e)
        {
           return (false, e.Message);
        }
    }

    public void GenerateTestData()
    {
        var hostAlex = new Host
        {
            FirstName = "Alex",
            LastName = "Shevchenko",
            Email = "alex@gmail.com",
            PhoneNumber = "+380978986655"
        };
        
        hostAlex.AddApartment(new Apartment
        {
            Address =  new Address { City = "Kyiv", Street = "Dilova", HouseNumber = "123" },
            Type = ApartmentType.Apartment,
            PricePerNight = 1500,
            NumberOfRooms = 1,
            IsAvailable = true
        });
        
        hostAlex.AddApartment(new Apartment
        {
            Address =  new Address { City = "Kozyn", Street = "Charivna", HouseNumber = "32" },
            Type = ApartmentType.PrivateHouse,
            PricePerNight = 1800,
            NumberOfRooms = 6,
            IsAvailable = false
        });
        
        var hostValeria = new Host
        {
            FirstName = "Valeria",
            LastName = "Kravchenko",
            Email = "kravalery@gmail.com",
            PhoneNumber = "+380566667678"
        };
        
        hostValeria.AddApartment(new Apartment
        {
            Address =  new Address { City = "Odessa", Street = "Heroiv Kosmonavtiv", HouseNumber = "132А" },
            Type = ApartmentType.Studio,
            PricePerNight = 1000,
            NumberOfRooms = 1,
            IsAvailable = true
        });
        
        var hostMaksym = new Host
        {
            FirstName = "Maksym",
            LastName = "Beliakov",
            Email = "maxbel@gmail.com",
            PhoneNumber = "+380671112233"
        };

        hostMaksym.AddApartment(new Apartment
        {
            Address = new Address { City = "Lviv", Street = "Svobody Avenue", HouseNumber = "15" },
            Type = ApartmentType.Apartment,
            PricePerNight = 2100,
            NumberOfRooms = 2,
            IsAvailable = true
        });

        hostMaksym.AddApartment(new Apartment
        {
            Address = new Address { City = "Bukovel", Street = "Karpatyska", HouseNumber = "7" },
            Type = ApartmentType.PrivateHouse,
            PricePerNight = 4500,
            NumberOfRooms = 5,
            IsAvailable = true
        });

        var hostOlena = new Host
        {
            FirstName = "Olena",
            LastName = "Tkachenko",
            Email = "olena.tk@gmail.com",
            PhoneNumber = "+380931234567"
        };

        hostOlena.AddApartment(new Apartment
        {
            Address = new Address { City = "Dnipro", Street = "Centralna", HouseNumber = "44" },
            Type = ApartmentType.Studio,
            PricePerNight = 950,
            NumberOfRooms = 1,
            IsAvailable = false
        });

        var hostAndrii = new Host
        {
            FirstName = "Andrii",
            LastName = "Melnyk",
            Email = "andmel@gmail.com",
            PhoneNumber = "+380991119988"
        };

        hostAndrii.AddApartment(new Apartment
        {
            Address = new Address { City = "Kharkiv", Street = "Naukova", HouseNumber = "81" },
            Type = ApartmentType.Apartment,
            PricePerNight = 1750,
            NumberOfRooms = 3,
            IsAvailable = true
        });

        hostAndrii.AddApartment(new Apartment
        {
            Address = new Address { City = "Poltava", Street = "Yevropeiska", HouseNumber = "9" },
            Type = ApartmentType.Studio,
            PricePerNight = 800,
            NumberOfRooms = 1,
            IsAvailable = true
        });
        
        var hostDmytro = new Host
        {
            FirstName = "Dmytro",
            LastName = "Dikiy",
            Email = "dmytrodik@gmail.com",
            PhoneNumber = "+380661231212"
        };

        hostDmytro.AddApartment(new Apartment
        {
            Address = new Address { City = "Uzhhorod", Street = "Kapushanska", HouseNumber = "101" },
            Type = ApartmentType.PrivateHouse,
            PricePerNight = 3200,
            NumberOfRooms = 4,
            IsAvailable = false
        });

        hostDmytro.AddApartment(new Apartment
        {
            Address = new Address { City = "Chernivtsi", Street = "Holovna", HouseNumber = "27" },
            Type = ApartmentType.Apartment,
            PricePerNight = 1488,
            NumberOfRooms = 2,
            IsAvailable = true
        });
        
        _repository.Add(hostAlex);
        _repository.Add(hostValeria);
        _repository.Add(hostMaksym);
        _repository.Add(hostOlena);
        _repository.Add(hostAndrii);
        _repository.Add(hostDmytro);

        _repository.Save();
    }
}