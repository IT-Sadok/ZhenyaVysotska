namespace BookingWeb.Application.Admin;

public sealed record UserFilterDto(
    string? Role,
    string? Email,
    string? SortBy,
    int Page = 1,
    int PageSize = 20);
    