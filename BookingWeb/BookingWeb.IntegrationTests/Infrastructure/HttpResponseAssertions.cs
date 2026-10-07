using System.Net;
using System.Text.Json;
using Shouldly;

namespace BookingWeb.IntegrationTests.Infrastructure;

public static class HttpResponseAssertions
{
    public static async Task ShouldHaveStatusAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode == expected)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(expected, $"Response body: {body}");
    }
    
    public static async Task<string?> ReadProblemTitleAsync(this HttpResponseMessage response)
    {
        using var document = await ReadJsonAsync(response);

        return document.RootElement.TryGetProperty("title", out var title)
            ? title.GetString()
            : null;
    }
    
    public static async Task<IReadOnlyList<string>> ReadValidationErrorFieldsAsync(this HttpResponseMessage response)
    {
        using var document = await ReadJsonAsync(response);

        if (!document.RootElement.TryGetProperty("errors", out var errors))
        {
            return [];
        }

        return errors.EnumerateObject().Select(property => property.Name).ToList();
    }
    
    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body);
    }

}