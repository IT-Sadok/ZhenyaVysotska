using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookingWeb.Application.Auth.Requests;
using BookingWeb.Application.Auth.Responses;
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
    
    protected HttpClient CreateAuthorizedClient(string accessToken)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}