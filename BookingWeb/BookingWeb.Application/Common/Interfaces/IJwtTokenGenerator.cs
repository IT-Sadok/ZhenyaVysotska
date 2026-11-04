namespace BookingWeb.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    TokenResult GenerateToken(Guid id, string email, IEnumerable<string> roles);
}
public sealed record TokenResult(string AccessToken, DateTime ExpiresAtUtc);