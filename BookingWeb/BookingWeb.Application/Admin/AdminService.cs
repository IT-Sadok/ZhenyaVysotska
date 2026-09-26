using BookingWeb.Application.Auth.Responses;
using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Models;
using BookingWeb.Application.Results;
using FluentValidation;

namespace BookingWeb.Application.Admin;

public sealed class AdminService
{
    private readonly IUserReadService _userReadService;
    private readonly IEnumerable<IValidator<UserFilterDto>> _userFilterValidators;

    public AdminService(IUserReadService userReadService, IEnumerable<IValidator<UserFilterDto>> userFilterValidators)
    {
        _userReadService = userReadService;
        _userFilterValidators = userFilterValidators;
    } 
    
    public async Task<Result<PagedResult<UserDto>>> GetUsersAsync(UserFilterDto filter, CancellationToken ct)
    { 
        var validationError = await _userFilterValidators.ValidateAllAsync(filter, ct);
        if(validationError is not null)
            return Result.Failure<PagedResult<UserDto>>(validationError);
        
        var page = await _userReadService.GetUsersAsync(filter, ct);
        return Result.Success(page);
    }
}