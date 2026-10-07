using System.Data.Common;
using BookingWeb.Application.Auth.Requests;
using BookingWeb.Application.Auth.Responses;
using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Models;
using BookingWeb.Application.Results;
using BookingWeb.Domain;
using FluentValidation;

namespace BookingWeb.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IIdentityService _identity;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IRefreshTokenService _refreshTokens; 
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IIdentityService identity, IJwtTokenGenerator jwt,
        IRefreshTokenService refreshTokens, IUnitOfWork unitOfWork)
    {
       _identity = identity;
       _jwt = jwt;
       _refreshTokens = refreshTokens;
       _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(
        RegisterRequest request, CancellationToken ct = default)
    {
        var result = await _identity.RegisterAsync(
            request.Email, request.Password, request.FirstName, request.LastName, request.Role, ct);

        if (result.IsFailure)
        {
            return result.Error;
        }
        
        return await BuildAuthResponseAsync(result.Value, ct);
    }
    
    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request, CancellationToken ct = default)
    {
        var result = await _identity.ValidateCredentialsAsync(request.Email, request.Password, ct);

        if (result.IsFailure)
        {
            return result.Error;
        }
        
        return await BuildAuthResponseAsync(result.Value, ct);
    }
    
    public async Task<Result<AuthResponse>> RefreshAsync(
        string refreshToken, CancellationToken ct = default)
    {
        var tokenValidation = await _refreshTokens.ValidateAsync(refreshToken, ct);
        if (tokenValidation.IsFailure)
            return tokenValidation.Error;

        var userId = tokenValidation.Value;

        var userResult = await _identity.GetUserByIdAsync(userId, ct);
        if (userResult.IsFailure)
        {
            await _refreshTokens.RevokeAllForUserAsync(userId, ct); 
            return userResult.Error;
        }

        await _refreshTokens.RevokeAsync(refreshToken, ct); 
        return await BuildAuthResponseAsync(userResult.Value, ct);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        await _refreshTokens.RevokeAsync(refreshToken, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<Result<AccessTokenResponse>> AddRoleAsync(
        Guid userId, AddRoleRequest request, CancellationToken ct = default)
    {
        var roleName = Roles.GetExactRoleName(request.Role) ?? request.Role;;
 
        var userResult = await _identity.AddToRoleAsync(userId, roleName, ct);
        if (userResult.IsFailure)
        {
            return userResult.Error;
        }
 
        var user = userResult.Value;
        
        var accessToken = _jwt.GenerateToken(user.Id, user.Email, user.Roles);

        return new AccessTokenResponse(accessToken.AccessToken, accessToken.ExpiresAtUtc);
    }
    
    private async Task<Result<AuthResponse>> BuildAuthResponseAsync(UserDto user, CancellationToken ct = default)
    {
        var access = _jwt.GenerateToken(user.Id, user.Email, user.Roles);
        var refresh = await _refreshTokens.IssueAsync(user.Id, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return new AuthResponse(access.AccessToken, access.ExpiresAtUtc, refresh.Token, refresh.ExpiresAtUtc);
    }
}