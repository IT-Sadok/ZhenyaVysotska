using BookingWeb.Application.Constants;
using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Models;
using BookingWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookingWeb.Infrastructure.Identity;

public sealed class UserReadService:IUserReadService
{
    private readonly ApplicationDbContext _db;
    
    public UserReadService(ApplicationDbContext db)
    {
        _db = db;
    }
    public async Task<PagedResult<UserDto>> GetUsersAsync(UserFilterDto filter, CancellationToken ct)
    {
        var query = _db.Users.AsNoTracking();
        
        if (!string.IsNullOrWhiteSpace(filter.Role))
        {
            query = query.Where(u => u.Roles.Any(r=> r.Name == filter.Role));
        }
        
        if (!string.IsNullOrWhiteSpace(filter.Email))
        {
            query = query.Where(u => u.Email != null && u.Email.Contains(filter.Email));
        }

        var totalCount = await query.CountAsync(ct);

        query = filter.SortBy switch
        {
            SortTokens.EmailAsc => query.OrderBy(u => u.Email),
            SortTokens.EmailDesc => query.OrderByDescending(u => u.Email),
            _ => query.OrderBy(u => u.Id)
        };

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(u => new UserDto(
                u.Id,
                u.Email!,
                u.Roles.Select(r=> r.Name!).ToList(),
                u.DefaultPersona
            )).ToListAsync(ct);
        
        return new PagedResult<UserDto>(items, totalCount, filter.Page, filter.PageSize);
    }
}