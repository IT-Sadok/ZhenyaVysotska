using System.Buffers.Text;
using System.Text.Json;

namespace BookingWeb.IntegrationTests.Infrastructure;

public static class TestJwt
{
    private const string RoleClaimName = "role";

    public static IReadOnlyList<string> ReadRoles(string accessToken)
    {
        var payloadSegment = accessToken.Split('.')[1];
        var payloadBytes = Base64Url.DecodeFromChars(payloadSegment);

        using var payload = JsonDocument.Parse(payloadBytes);

        if (!payload.RootElement.TryGetProperty(RoleClaimName, out var claim))
        {
            return [];
        }

        if (claim.ValueKind == JsonValueKind.Array)
        {
            return claim.EnumerateArray()
                .Select(item => item.GetString()!)
                .ToList();
        }
        
        return [claim.GetString()!];
    }
}