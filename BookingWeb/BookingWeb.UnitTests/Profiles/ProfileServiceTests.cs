using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Profiles;
using BookingWeb.Application.Profiles.Requests;
using BookingWeb.Application.Results;
using BookingWeb.Domain.Models;
using FluentValidation;
using Moq;
using Shouldly;

namespace BookingWeb.UnitTests.Profiles;

public class ProfileServiceTests
{
    private readonly Mock<IUserProfileRepository> _profiles = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    
    private ProfileService CreateSut() => new(
        new IValidator<UpdateProfileRequest>[]
        {
            new UpdateProfileRequestValidator()
        },
        _profiles.Object,
        _unitOfWork.Object);

    private static readonly Guid UserId = Guid.NewGuid();

    private static UpdateProfileRequest ValidUpdate() =>
        new("Oksana", "Koval", "Нове біо", "https://example.com/new.png");


    [Fact]
    public async Task Get_missing_profile_returns_not_found()
    {
        _profiles.Setup(p => p.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var result = await CreateSut().GetAsync(UserId);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Profile.NotFound");
    }

    [Fact]
    public async Task Get_existing_profile_returns_dto()
    {
        var profile = UserProfile.Create(UserId, "Ivan", "Petrenko");
        _profiles.Setup(p => p.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await CreateSut().GetAsync(UserId);

        result.IsSuccess.ShouldBeTrue();
        result.Value.UserId.ShouldBe(UserId);
        result.Value.FirstName.ShouldBe("Ivan");
        result.Value.LastName.ShouldBe("Petrenko");
    }

    [Fact]
    public async Task Update_invalid_input_skips_repo_and_save()
    {
        var badRequest = ValidUpdate() with { FirstName = "" };

        var result = await CreateSut().UpdateAsync(UserId, badRequest);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<ValidationError>();
        _profiles.Verify(p => p.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_missing_profile_returns_not_found_and_does_not_save()
    {
        _profiles.Setup(p => p.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var result = await CreateSut().UpdateAsync(UserId, ValidUpdate());

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Profile.NotFound");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_success_applies_changes_and_saves_once()
    {
        var profile = UserProfile.Create(UserId, "Old", "Name");
        _profiles.Setup(p => p.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await CreateSut().UpdateAsync(UserId, ValidUpdate());

        result.IsSuccess.ShouldBeTrue();
        result.Value.FirstName.ShouldBe("Oksana");
        result.Value.LastName.ShouldBe("Koval");
        result.Value.Bio.ShouldBe("Нове біо");

        profile.FirstName.ShouldBe("Oksana");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

}