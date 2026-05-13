using Booking.Domain.Models;

namespace Booking.Domain.Interfaces;

public interface IHostRepository
{
    List<Host> GetAll();
    Host GetById(int id);
    void Add(Host host);
    void Update(Host host);
    void Delete(int id);
    void Save();
}