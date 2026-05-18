using Booking.Business.Services;
using Booking.Data;
using Booking.Domain.Interfaces;

namespace Booking.Presentation;

class Program
{
    static void Main(string[] args)
    {
        IHostRepository repository = new FileHostRepository();
        HostService service = new HostService(repository);
        Menu menu = new Menu(service);
        menu.Run();
    }
}