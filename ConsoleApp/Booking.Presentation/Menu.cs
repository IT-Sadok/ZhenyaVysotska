using Booking.Business.DTOs;
using Booking.Business.Services;
using Booking.Domain.Models;

namespace Booking.Presentation
{
    public class Menu
    {
        private readonly HostService _hostService;

        public Menu(HostService hostService)
        {
            _hostService = hostService;
        }

        public void Run()
        {
            if (_hostService.GetAllHosts().Count == 0)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.WriteLine("Warning: Your file is empty (or file not found).");
                Console.ResetColor();
                Console.WriteLine("Do you want to add test data in file? (y/n)");
                
                string answer = Console.ReadLine()?.ToLower();
                if (answer == "y" || answer == "н")
                {
                    _hostService.GenerateTestData();
                    PrintSuccess("Test data created successfully.");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
            }
            
            bool exit = false;
            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("----Booking system----");
                Console.WriteLine("1. Create new host");
                Console.WriteLine("2. Add apartment to host");
                Console.WriteLine("3. Show hosts");
                Console.WriteLine("4. Save file");
                Console.WriteLine("5. Update host information");
                Console.WriteLine("6. Remove host");
                Console.WriteLine("7. Simulate Race Condition");
                Console.WriteLine("8. Update price by 2 hosts safely ");
                Console.WriteLine("0. Exit");
                Console.Write("\nYour choice: ");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        CreateHostUI();
                        break;
                    case "2":
                        AddApartmentUI();
                        break;
                    case "3":
                        ShowHostsUI();
                        break;
                    case "4":
                        var saveResult = _hostService.SaveToFile();
                        PrintResult(saveResult.IsSuccess,  saveResult.Message);
                        break;
                    case "5":
                        UpdateHostUI();
                        break;
                    case "6":
                        DeleteHostUI();
                        break;
                    case "7":
                        SimulateRaceConditionUI();
                        break;
                    case "8":
                        UpdatePriceByTwoHostsUI();
                        break;
                    case "0":
                        exit = true;
                        PrintSuccess("Thank you for using the Booking System!");
                        break;
                    default:
                        PrintError("Unknown command. Try again!");
                        break;
                }
                if (!exit)
                {
                    Console.WriteLine("\nPlease, press any button to return to the menu.");
                    Console.ReadKey(); 
                }
            }
        }

        private void CreateHostUI()
        {
            Console.Clear();
            Console.WriteLine("Creating new host");

            var request = new AddHostDto();
            
            Console.Write("Enter first name: ");
            request.FirstName = Console.ReadLine();
            
            Console.Write("Enter last name: ");
            request.LastName= Console.ReadLine();
            
            Console.Write("Enter email: ");
            request.Email = Console.ReadLine();
            
            Console.Write("Enter the phone number: ");
            request.PhoneNumber = Console.ReadLine();

            var result = _hostService.CreateHost(request);
            PrintResult(result.IsSuccess, result.Message);
        }

        private void AddApartmentUI()
        {
            Console.Clear();
            Console.WriteLine("Adding apartment(s)");
            
            List<int> hostIds = [];
            while (true)
            {
                int hostId = ReadInt("Enter the host ID (or 0 to exit): ");
                if (hostId == 0) break;
                hostIds.Add(hostId);
            }
            
            var request = new AddApartmentDto
            {
                HostIds = hostIds,
                Address = new Address() 
            };

            Console.WriteLine("\nAddress");
            Console.Write("City: ");
            request.Address.City = Console.ReadLine();
            Console.Write("Street: ");
            request.Address.Street = Console.ReadLine();
            Console.Write("House number: ");
            request.Address.HouseNumber = Console.ReadLine();
         
            Console.WriteLine("Type (1 - Apartment, 2 - Studio, 3 - Private House): ");
            
            int typeChoice = ReadInt("Your choice: ");
            request.Type = (ApartmentType)typeChoice; 

            request.PricePerNight = ReadDecimal("Price per night: ");
            request.NumberOfRooms = ReadInt("Number of rooms: ");
            request.IsAvailable = true;

            var result = _hostService.AddApartmentToHost(request);
            PrintResult(result.IsSuccess, result.Message);
        }
        
        private void ShowHostsUI()
        {
            Console.Clear();
            Console.WriteLine("Host list: ");
            
            var hosts = _hostService.GetAllHosts();
            
            if (hosts.Count == 0)
            {
                Console.WriteLine("List is empty.");
                return;
            }

            foreach (var host in hosts)
            {
                Console.WriteLine($"ID: {host.Id} | {host.FirstName} {host.LastName} | Phone: {host.PhoneNumber} | email: {host.Email}");
            }
            
            int hostId = ReadInt("Enter the host ID to view their apartments (or 0 to return to the menu): ");
            
            if (hostId == 0) return;
            
            var selectedHost =  _hostService.GetHostById(hostId);
            if (selectedHost == null)
            {
                PrintError("Host not found.");
                return;
            }
            
            Console.Clear();
            Console.WriteLine($"Host's apartments: {selectedHost.FirstName} {selectedHost.LastName}");
    
            if (selectedHost.Apartments.Count == 0)
            {
                Console.WriteLine("This host has no apartments yet.");
                return;
            }

            foreach (var apt in selectedHost.Apartments)
            {
                string location = apt.Address != null ? $"{apt.Address.City}, {apt.Address.Street}" : "Address is unknown";
                Console.WriteLine($"ID: {apt.Id} | Type: {apt.Type} | Address: {location} | Price: {apt.PricePerNight} | Number of rooms: {apt.NumberOfRooms}");
            }
            
        }

        public void UpdateHostUI()
        {
            Console.Clear();
            Console.WriteLine("Updating host");
            int hostId = ReadInt("Enter host ID: ");
            
            var host = _hostService.GetHostById(hostId);
            if (host == null)
            {
                PrintError("Host not found.");
                return;
            }

            Console.WriteLine("Enter new data or press 'Enter' to keep the old value");
            
            Console.Write($"Old last name: {host.LastName} | New last name: ");
            string newLastName = Console.ReadLine();
            newLastName = string.IsNullOrWhiteSpace(newLastName) ? host.LastName : newLastName;
            
            Console.Write($"Old first name: {host.FirstName} | New first name: ");
            string newFirstName = Console.ReadLine();
            newFirstName = string.IsNullOrWhiteSpace(newFirstName) ? host.FirstName : newFirstName;
            
            Console.Write($"Old email: {host.Email} | New email: ");
            string newEmail = Console.ReadLine();
            newEmail = string.IsNullOrWhiteSpace(newEmail) ? host.Email : newEmail;
            
            Console.Write($"Old phone number: {host.PhoneNumber} | New phone number: ");
            string newPhoneNumber = Console.ReadLine();
            newPhoneNumber = string.IsNullOrWhiteSpace(newPhoneNumber) ? host.PhoneNumber : newPhoneNumber;

            var request = new UpdateHostDto
            {
                Id = hostId,
                FirstName = newFirstName,
                LastName = newLastName,
                Email = newEmail,
                PhoneNumber = newPhoneNumber
            };
            
            var result = _hostService.UpdateHost(request);
            PrintResult(result.IsSuccess, result.Message);
        }
        
        private void DeleteHostUI()
        {
            Console.Clear();
            Console.WriteLine("Deleting host");
    
            int id = ReadInt("Enter host ID or 0 to cancel the operation: ");
            if (id == 0) return;
            
            var result = _hostService.DeleteHost(id); 
            PrintResult(result.IsSuccess, result.Message);
        }

        private void SimulateRaceConditionUI()
        {
            Console.Clear();
            Console.WriteLine("Simulating race condition || Updating price in concurrent threads");
            ShowHostsUI();
            
            int hostId = ReadInt("Enter host ID: ");
            var host = _hostService.GetHostById(hostId);
            
            if (host == null)
            {
                Console.WriteLine("Host not found.");
                Console.ReadKey();
                return;
            }
            
            int apartmentId = ReadInt("Enter Apartment ID: ");
            var apartment = host.Apartments.FirstOrDefault(a => a.Id == apartmentId);
            
            if (apartment == null)
            {
                Console.WriteLine("Apartment not found.");
                Console.ReadKey();
                return;
            }
            
            Console.WriteLine($"\nInitial Price: {apartment.PricePerNight}");
            Console.WriteLine($"Expected Price: {apartment.PricePerNight + 20} (if it worked correctly)");
            Console.WriteLine("Starting 2 parallel threads to increase price by 10 each...");
            Console.WriteLine("Please wait 3 seconds...\n");
            
            Task[] tasks = new Task[2];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() => apartment.IncreasePriceWithRaceCondition(10));
            }
            
            Task.WaitAll(tasks);
            
            Console.WriteLine($"Actual Final Price: {apartment.PricePerNight}  <-- ERROR! RACE CONDITION!");

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        private void UpdatePriceByTwoHostsUI()
        {
            Console.Clear();
            Console.WriteLine("Updating price in 2 concurrent threads safely");
            ShowHostsUI();
            
            int hostId = ReadInt("Enter host ID: ");
            var host = _hostService.GetHostById(hostId);
            
            if (host == null)
            {
                Console.WriteLine("Host not found.");
                Console.ReadKey();
                return;
            }
            
            int apartmentId = ReadInt("Enter Apartment ID: ");
            var apartment = host.Apartments.FirstOrDefault(a => a.Id == apartmentId);
            
            if (apartment == null)
            {
                Console.WriteLine("Apartment not found.");
                Console.ReadKey();
                return;
            }
            
            Console.WriteLine($"\nInitial Price: {apartment.PricePerNight}");
            Console.WriteLine($"Expected Price: {apartment.PricePerNight + 20} (if it worked correctly)");
            Console.WriteLine("Starting 2 parallel threads to increase price by 10 each...");
            Console.WriteLine("Please wait 3 seconds...\n");
            
            Task[] tasks = new Task[2];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() => apartment.IncreasePriceSafely(10));
            }
            
            Task.WaitAll(tasks);
            Console.WriteLine($"Actual Final Price: {apartment.PricePerNight}");

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
        
        private int ReadInt(string prompt)
        {
            int result;
            while (true) 
            {
                Console.Write(prompt);
                if (int.TryParse(Console.ReadLine(), out result))
                {
                    return result;
                }
                PrintError("Invalid input. Enter an integer.");
            }
        }

        private decimal ReadDecimal(string prompt)
        {
            decimal result;
            while (true)
            {
                Console.Write(prompt);
                if (decimal.TryParse(Console.ReadLine(), out result) && result >= 0)
                {
                    return result;
                }
                PrintError("Invalid input. Enter an number (number >= 0).");
            }
        }

        private void PrintResult(bool isSuccess, string message)
        {
            if (isSuccess)
                PrintSuccess(message);
            else
                PrintError(message);
        }

        private void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor(); 
        }

        private void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}