using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookingWeb.Application.Auth.Requests;
using BookingWeb.Application.Auth.Responses;
using BookingWeb.Application.Listings;
using BookingWeb.Application.Listings.Requests;
using BookingWeb.Domain;

namespace BookingWeb.IntegrationTests.Infrastructure;

[Collection(ApiCollection.Name)]
public abstract class ApiTestBase : IAsyncLifetime
{
    protected const string DefaultPassword = "Password1!";

    protected ApiTestBase(ApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected ApiFactory Factory { get; }
    
    protected HttpClient Client { get; }

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }
    
    protected async Task<AuthResponse> RegisterAsync(
        string email,
        string role = Roles.Client,
        string firstName = "Ivan",
        string lastName = "Petrenko")
    {
        var request = new RegisterRequest(email, DefaultPassword, firstName, lastName, role);

        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    protected async Task<AuthResponse> LoginAsync(string email, string password = DefaultPassword)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    protected Task<AuthResponse> LoginAsAdminAsync()
    {
        return LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);
    }
    
    protected static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
    
    protected async Task<HttpClient> CreateUserClientAsync(string email, string role)
    {
        var auth = await RegisterAsync(email, role);
        return CreateAuthorizedClient(auth.AccessToken);
    }

    protected static async Task<ListingDto> CreateListingAsync(
        HttpClient hostClient,
        decimal pricePerNight = 1000m,
        int maxGuests = 4,
        string city = "Kyiv")
    {
        var request = new ListingRequest("Loft", "Cozy loft", city, "Khreshchatyk 1", pricePerNight, maxGuests);

        var response = await hostClient.PostAsJsonAsync("/api/listings", request);

        await response.ShouldHaveStatusAsync(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ListingDto>())!;
    }
    
    protected HttpClient CreateAuthorizedClient(string accessToken)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}