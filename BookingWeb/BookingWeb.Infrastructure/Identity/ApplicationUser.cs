using Microsoft.AspNetCore.Identity;

namespace BookingWeb.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? DefaultPersona { get; set; }
}