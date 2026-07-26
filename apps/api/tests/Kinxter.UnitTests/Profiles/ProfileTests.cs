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

    [Theory]
    [InlineData("ab")]
    [InlineData("admin")]
    [InlineData("name-with-dash")]
    [InlineData(".hidden")]
    public void Create_rejects_invalid_or_reserved_handles(string handle)
    {
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() => Profile.Create(
            Guid.CreateVersion7(now),
            Guid.CreateVersion7(now),
            handle,
            "Display name",
            now));
    }

    [Fact]
    public void MarkOnboardingCompleted_requires_explicit_visibility()
    {
        var now = DateTimeOffset.UtcNow;
        var profile = Profile.Create(
            Guid.CreateVersion7(now),
            Guid.CreateVersion7(now),
            "valid_handle",
            "Display name",
            now);

        Assert.Throws<InvalidOperationException>(() => profile.MarkOnboardingCompleted(now));

        profile.SetVisibility(ProfileVisibility.Private, now);
        profile.MarkOnboardingCompleted(now);

        Assert.Equal(ProfileVisibility.Private, profile.Visibility);
        Assert.Equal(now, profile.OnboardingCompletedAt);
    }

    [Fact]
    public void UpdateDetails_changes_and_normalizes_handle()
    {
        var now = DateTimeOffset.UtcNow;
        var profile = Profile.Create(
            Guid.CreateVersion7(now),
            Guid.CreateVersion7(now),
            "first.handle",
            "Display name",
            now);

        profile.UpdateDetails("New_Handle", "New display", null, null, now.AddMinutes(1));

        Assert.Equal("New_Handle", profile.Handle);
        Assert.Equal("new_handle", profile.NormalizedHandle);
    }
}
