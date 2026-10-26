namespace BookingWeb.Application.Models;

public sealed record PageRequest(int Page = 1, int PageSize = 20);