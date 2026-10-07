using BookingWeb.Application.Models;

namespace BookingWeb.Application.Interfaces;

public interface IJwtTokenGenerator
{
    TokenResult GenerateToken(Guid id, string email, IEnumerable<string> roles);
}
public sealed record TokenResult(string AccessToken, DateTime ExpiresAtUtc);