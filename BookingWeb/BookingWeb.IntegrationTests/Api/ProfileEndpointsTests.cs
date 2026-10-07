using System.Net;
using System.Net.Http.Json;
using BookingWeb.Application.Models;
using BookingWeb.Application.Profiles.Requests;
using BookingWeb.IntegrationTests.Infrastructure;
using Shouldly;

namespace BookingWeb.IntegrationTests.Api;

public sealed class ProfileEndpointsTests : ApiTestBase
{
    private const string ProfileUrl = "/api/profile";

    public ProfileEndpointsTests(ApiFactory factory) : base(factory)
    {
        
    }

    [Fact]
    public async Task GetProfile_ShouldReturnNamesFromRegistration_WhenUserIsRegistered()
    {
        var auth = await RegisterAsync("ivan@test.com", firstName: "Ivan", lastName: "Petrenko");
        using var client = CreateAuthorizedClient(auth.AccessToken);

        var profile = await client.GetFromJsonAsync<ProfileDto>(ProfileUrl);

        profile.ShouldNotBeNull();
        profile.FirstName.ShouldBe("Ivan");
        profile.LastName.ShouldBe("Petrenko");
        profile.Bio.ShouldBeNull();
        profile.AvatarUrl.ShouldBeNull();
    }

    [Fact]
    public async Task GetProfile_ShouldReturnUnauthorized_WhenTokenIsMissing()
    {
        var response = await Client.GetAsync(ProfileUrl);

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_ShouldPersistChanges_WhenRequestIsValid()
    {
        var auth = await RegisterAsync("ivan@test.com");
        using var client = CreateAuthorizedClient(auth.AccessToken);
        var request = new UpdateProfileRequest("  Oksana  ", "Koval", "Loves travelling", "https://example.com/a.png");

        var updateResponse = await client.PutAsJsonAsync(ProfileUrl, request);
        await updateResponse.ShouldHaveStatusAsync(HttpStatusCode.OK);

        var profile = await client.GetFromJsonAsync<ProfileDto>(ProfileUrl);
        profile.ShouldNotBeNull();
        profile.FirstName.ShouldBe("Oksana");
        profile.LastName.ShouldBe("Koval");
        profile.Bio.ShouldBe("Loves travelling");
        profile.AvatarUrl.ShouldBe("https://example.com/a.png");
    }

    [Fact]
    public async Task UpdateProfile_ShouldReturnValidationProblemAndKeepProfile_WhenFirstNameIsEmpty()
    {
        var auth = await RegisterAsync("ivan@test.com", firstName: "Ivan");
        using var client = CreateAuthorizedClient(auth.AccessToken);

        var response = await client.PutAsJsonAsync(
            ProfileUrl, new UpdateProfileRequest("", "Koval", null, null));

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        var fields = await response.ReadValidationErrorFieldsAsync();
        fields.ShouldContain(field => field.Equals("FirstName", StringComparison.OrdinalIgnoreCase));

        var profile = await client.GetFromJsonAsync<ProfileDto>(ProfileUrl);
        profile!.FirstName.ShouldBe("Ivan");
    }
}
