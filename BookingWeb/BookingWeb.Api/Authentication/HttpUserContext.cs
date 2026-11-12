using BookingWeb.Api.Extensions;
using BookingWeb.Application.Common.Interfaces;

namespace BookingWeb.Api.Authentication;

public sealed class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpUserContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }
    
    public Guid UserId =>
        _accessor.HttpContext?.User.GetUserId()
        ?? throw new InvalidOperationException(
            "No user id in the current request. Is the endpoint missing RequireAuthorization()?");
}
