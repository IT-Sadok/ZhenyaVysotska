using System.Data.Common;
using BookingWeb.Application.Auth.Requests;
using BookingWeb.Application.Auth.Responses;
using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Models;
using BookingWeb.Application.Results;
using BookingWeb.Domain;
using FluentValidation;

namespace BookingWeb.Application.Auth;

public sealed class AuthService
{
    private readonly IIdentityService _identity;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IRefreshTokenService _refreshTokens; 
    private readonly IEnumerable<IValidator<RegisterRequest>> _registerValidators;
    private readonly IEnumerable<IValidator<LoginRequest>> _loginValidators;

    public AuthService(
        IIdentityService identity, IJwtTokenGenerator jwt,
        IRefreshTokenService refreshTokens,
        IEnumerable<IValidator<RegisterRequest>> registerValidators,
        IEnumerable<IValidator<LoginRequest>> loginValidators)
    {
       _identity = identity;
       _jwt = jwt;
       _refreshTokens = refreshTokens;
       _registerValidators = registerValidators;
       _loginValidators = loginValidators;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(
        RegisterRequest request, CancellationToken token = default)
    {
        var validationError = await _registerValidators.ValidateAllAsync(request, token);
        if(validationError is not null)
            return Result.Failure<AuthResponse>(validationError);
        
        var result = await _identity.RegisterAsync(request.Email, request.Password,  request.FirstName,
            request.LastName, request.Role, token);
        
        return result.IsFailure 
            ? Result.Failure<AuthResponse>(result.Error) 
            : await BuildAuthResponseAsync(result.Value, ResolveActivePersona(result.Value), token);
    }
    
    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request, CancellationToken token = default)
    {
        var validationError = await _loginValidators.ValidateAllAsync(request, token);
        if(validationError is not null)
            return Result.Failure<AuthResponse>(validationError);
        
        var result = await _identity.ValidateCredentialsAsync(
            request.Email, request.Password, token);

        return result.IsFailure
            ? Result.Failure<AuthResponse>(result.Error)
            : await BuildAuthResponseAsync(result.Value, ResolveActivePersona(result.Value), token);
    }
    
    public async Task<Result<AuthResponse>> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var validation = await _refreshTokens.ValidateAsync(refreshToken, ct);
        if (validation.IsFailure)
            return Result.Failure<AuthResponse>(validation.Error);

        var userResult = await _identity.GetActiveUserAsync(validation.Value, ct);
        if (userResult.IsFailure)
        {
            await _refreshTokens.RevokeAllForUserAsync(validation.Value, ct); 
            return Result.Failure<AuthResponse>(userResult.Error);
        }

        await _refreshTokens.RevokeAsync(refreshToken, ct); 
        return await BuildAuthResponseAsync(userResult.Value, ResolveActivePersona(userResult.Value), ct);
    }
    
    public async Task<Result<AccessTokenResponse>> EnablePersonaAsync(
        Guid userId, string persona, CancellationToken ct = default)
    {
        if (!Personas.IsSwitchable(persona))
            return Result.Failure<AccessTokenResponse>(
                new Error("Persona.Invalid", $"'{persona}' can't be switchable."));

        var added = await _identity.AddToRoleAsync(userId, persona, ct);
        if (added.IsFailure)
            return Result.Failure<AccessTokenResponse>(added.Error);

        var setDefault = await _identity.SetDefaultPersonaAsync(userId, persona, ct);
        return setDefault.IsFailure
            ? Result.Failure<AccessTokenResponse>(setDefault.Error)
            : BuildAccessToken(setDefault.Value, persona);
    }
    
    public async Task<Result<AccessTokenResponse>> SwitchPersonaAsync(
        Guid userId, string persona, CancellationToken ct = default)
    {
        var result = await _identity.SetDefaultPersonaAsync(userId, persona, ct);
        return result.IsFailure
            ? Result.Failure<AccessTokenResponse>(result.Error)
            : BuildAccessToken(result.Value, persona);
    }

    private async Task<Result<AuthResponse>> BuildAuthResponseAsync(
        UserDto user, string activePersona, CancellationToken token = default)
    {
        var access = _jwt.GenerateToken(user.Id, user.Email, user.Roles, activePersona);
        var refresh = await _refreshTokens.IssueAsync(user.Id, token);
        return Result.Success(new AuthResponse(access.AccessToken, access.ExpiresAtUtc, 
            refresh.Token, refresh.ExpiresAtUtc));
    }

    private Result<AccessTokenResponse> BuildAccessToken(UserDto user, string activePersona)
    {
        var access = _jwt.GenerateToken(user.Id, user.Email, user.Roles, activePersona);
        return Result.Success(new AccessTokenResponse(access.AccessToken, access.ExpiresAtUtc));
    }

    private static string ResolveActivePersona(UserDto user)
    {
        if (user.DefaultPersona is not null
            && Personas.IsSwitchable(user.DefaultPersona)
            && user.Roles.Contains(user.DefaultPersona))
            return user.DefaultPersona;

        if (user.Roles.Contains(Roles.Host)) return Roles.Host;
        if (user.Roles.Contains(Roles.Client)) return Roles.Client;
        return user.Roles.FirstOrDefault() ?? Roles.Client; 
    }
}