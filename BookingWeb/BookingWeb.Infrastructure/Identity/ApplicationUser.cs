using Microsoft.AspNetCore.Identity;

namespace BookingWeb.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public ICollection<IdentityRole<Guid>> Roles { get; private set; } = new List<IdentityRole<Guid>>();
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public bool IsDeleted => DeletedAtUtc is not null;
    
    public void MarkDeleted(DateTimeOffset deletedAtUtc)
    {
        var anonymized = $"deleted-{Id:N}@deleted.invalid";

        Email = anonymized;
        UserName = anonymized;
        EmailConfirmed = false;
        PhoneNumber = null;
        PhoneNumberConfirmed = false;
        PasswordHash = null;
        SecurityStamp = Guid.NewGuid().ToString();
        
        DeletedAtUtc = deletedAtUtc;
    }

}