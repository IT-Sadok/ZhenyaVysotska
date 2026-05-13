using System.Text.Json;
using Booking.Domain.Interfaces;
using Booking.Domain.Models;

namespace Booking.Data;

public class FileHostRepository : IHostRepository
{
    private List<Host> _hosts = new List<Host>();
    private readonly string _filePath = "hosts.json";
    private int _nextId = 1;

    public FileHostRepository()
    {
        if (File.Exists(_filePath))
        {
            LoadFromFile();
        }
        else
        {
            _hosts = new List<Host>();
        } 
    }

    private void LoadFromFile()
    {
        string jsonString = File.ReadAllText(_filePath);
        
        _hosts = JsonSerializer.Deserialize<List<Host>>(jsonString) ?? new List<Host>();

        if (_hosts.Any())
        {
            _nextId = _hosts.Max(h => h.Id) + 1;
        }
    }

    public void Save()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        
        string jsonString = JsonSerializer.Serialize(_hosts, options);
        File.WriteAllText(_filePath, jsonString);
    }
    
    public void Add(Host host)
    {
        if (host is null)
            throw new ArgumentNullException(nameof(host));
        
        host.Id = _nextId++;
        _hosts.Add(host);
    }

    public void Update(Host host)
    {
        if (host is null)
            throw new ArgumentNullException(nameof(host));
        
        int  index = _hosts.FindIndex(h => h.Id == host.Id);

        if (index != -1)
        {
            _hosts[index] = host;
        }
    }

    public void Delete(int id)
    {
        if(!_hosts.Any(h => h.Id ==id))
            throw new ArgumentException($"Host with id {id} does not exist");
        
        _hosts.RemoveAll(h => h.Id == id);
    }

    public List<Host> GetAll()
    {
        return _hosts;
    }

    public Host GetById(int id)
    {
        return _hosts.FirstOrDefault(x => x.Id == id);
    }
}