using BookingWeb.Application.Models;
using BookingWeb.Application.Results;

namespace BookingWeb.Application.Interfaces;

public interface IIdentityService
{
    
    Task<Result<UserDto>> RegisterAsync(
        string email, string password, string firstName, string lastName,string role, 
        CancellationToken token = default);

    Task<Result<UserDto>> ValidateCredentialsAsync(
        string email, string password, CancellationToken token = default);
    
    Task<Result<UserDto>> AddToRoleAsync(Guid userId, string role, CancellationToken token = default);

    Task<Result<UserDto>> SetDefaultPersonaAsync(Guid userId, string persona, CancellationToken token = default);

    Task<Result<UserDto>> GetActiveUserAsync(Guid userId, CancellationToken token = default);
}