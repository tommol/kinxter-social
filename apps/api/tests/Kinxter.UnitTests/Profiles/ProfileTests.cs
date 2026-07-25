using Kinxter.Profiles.Model;
using Xunit;

namespace Kinxter.UnitTests.Profiles;

public sealed class ProfileTests
{
    [Fact]
    public void Create_requires_handle_and_display_name()
    {
        var now = DateTimeOffset.UtcNow;

        var profile = Profile.Create(
            Guid.CreateVersion7(now),
            Guid.CreateVersion7(now),
            " Tomasz ",
            " Tomasz Molis ",
            now);

        Assert.Equal("Tomasz", profile.Handle);
        Assert.Equal("tomasz", profile.NormalizedHandle);
        Assert.Equal("Tomasz Molis", profile.DisplayName);
        Assert.Null(profile.OnboardingCompletedAt);
    }

    [Fact]
    public void CompleteOnboarding_sets_additional_profile_details()
    {
        var now = DateTimeOffset.UtcNow;
        var completedAt = now.AddMinutes(1);
        var profile = Profile.Create(
            Guid.CreateVersion7(now),
            Guid.CreateVersion7(now),
            "tomasz",
            "Tomasz Molis",
            now);

        profile.CompleteOnboarding(
            " Software builder ",
            " https://cdn.example.com/profiles/tomasz.jpg ",
            completedAt);

        Assert.Equal("Software builder", profile.Bio);
        Assert.Equal("https://cdn.example.com/profiles/tomasz.jpg", profile.ProfilePictureUrl);
        Assert.Equal(completedAt, profile.OnboardingCompletedAt);
        Assert.Equal(completedAt, profile.UpdatedAt);
    }
}
