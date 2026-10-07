using BookingWeb.Application.Auth.Requests;
using BookingWeb.Application.Auth.Responses;
using BookingWeb.Application.Results;

namespace BookingWeb.Application.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    
    Task<Result<AuthResponse>> RefreshAsync(string refreshToken, CancellationToken ct = default);
    
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
    
    Task<Result<AccessTokenResponse>> AddRoleAsync(Guid userId, AddRoleRequest request, CancellationToken ct = default);
}