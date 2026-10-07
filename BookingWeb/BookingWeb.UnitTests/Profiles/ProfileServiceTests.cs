using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Profiles;
using BookingWeb.Application.Profiles.Requests;
using BookingWeb.Application.Results;
using BookingWeb.Domain.Models;
using Moq;
using Shouldly;

public sealed class ProfileServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly Mock<IUserProfileRepository> _profiles = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ProfileService _sut;

    public ProfileServiceTests()
    {
        _sut = new ProfileService(_profiles.Object, _unitOfWork.Object);
    }

    private UserProfile ArrangeExistingProfile()
    {
        var profile = UserProfile.Create(UserId, "Ivan", "Petrenko");

        _profiles
            .Setup(repository => repository.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        return profile;
    }

    private void ArrangeMissingProfile()
    {
        _profiles
            .Setup(repository => repository.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);
    }

    [Fact]
    public async Task Get_ShouldReturnProfile_WhenProfileExists()
    {
        ArrangeExistingProfile();

        var result = await _sut.GetAsync(UserId);

        result.IsSuccess.ShouldBeTrue();
        result.Value.UserId.ShouldBe(UserId);
        result.Value.FirstName.ShouldBe("Ivan");
        result.Value.LastName.ShouldBe("Petrenko");
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenProfileIsMissing()
    {
        ArrangeMissingProfile();

        var result = await _sut.GetAsync(UserId);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Profile.NotFound");
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }
    
    [Fact]
    public async Task Update_ShouldApplyChangesToProfile_WhenProfileExists()
    {
        var profile = ArrangeExistingProfile();

        var result = await _sut.UpdateAsync(
            UserId, new UpdateProfileRequest("Oksana", "Koval", "Loves travelling", "https://example.com/a.png"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.FirstName.ShouldBe("Oksana");
        result.Value.Bio.ShouldBe("Loves travelling");
        profile.LastName.ShouldBe("Koval");
        profile.AvatarUrl.ShouldBe("https://example.com/a.png");
    }

    [Fact]
    public async Task Update_ShouldSaveChangesOnce_WhenProfileExists()
    {
        ArrangeExistingProfile();

        await _sut.UpdateAsync(UserId, new UpdateProfileRequest("Oksana", "Koval", null, null));

        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFoundWithoutSaving_WhenProfileIsMissing()
    {
        ArrangeMissingProfile();

        var result = await _sut.UpdateAsync(UserId, new UpdateProfileRequest("Oksana", "Koval", null, null));

        result.Error.Code.ShouldBe("Profile.NotFound");
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
