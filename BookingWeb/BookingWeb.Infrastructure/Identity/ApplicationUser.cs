using Microsoft.AspNetCore.Identity;

namespace BookingWeb.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public ICollection<IdentityRole<Guid>> Roles { get; private set; } = new List<IdentityRole<Guid>>();
}