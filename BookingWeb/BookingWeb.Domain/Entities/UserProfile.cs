using Ardalis.GuardClauses;

namespace BookingWeb.Domain.Entities;

public class UserProfile
{
    public Guid UserId { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string? Bio { get; private set; }
    public string? AvatarUrl { get; private set; }
    
    private UserProfile() {}

    public static UserProfile Create(Guid userId, string firstName, string lastName)
    {
        Guard.Against.Default(userId);
        var profile = new UserProfile {UserId =  userId};
        profile.UpdateName(firstName, lastName);
        return profile;
    }

    public void UpdateName(string firstName, string lastName)
    {
        Guard.Against.NullOrWhiteSpace(firstName);
        Guard.Against.NullOrWhiteSpace(lastName);
        
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }

    public void UpdateBio(string? bio)
    {
        Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
    }
    
    public void UpdateAvatar(string? avatarUrl)
    {
        AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
    }
}