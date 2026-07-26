namespace Kinxter.Profiles.Model;

public sealed class Profile
{
    public const int HandleMinLength = 3;
    public const int HandleMaxLength = 32;
    public const int HandleStorageMaxLength = 64;
    public const int DisplayNameMaxLength = 120;
    public const int BioMaxLength = 500;
    public const int ProfilePictureUrlMaxLength = 2048;

    private Profile()
    {
        Handle = null!;
        NormalizedHandle = null!;
        DisplayName = null!;
    }

    private Profile(
        Guid id,
        Guid accountId,
        string handle,
        string displayName,
        DateTimeOffset createdAt)
    {
        Id = id;
        AccountId = accountId;
        Handle = handle;
        NormalizedHandle = NormalizeHandle(handle);
        DisplayName = displayName;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public string Handle { get; private set; }

    public string NormalizedHandle { get; private set; }

    public string DisplayName { get; private set; }

    public string? Bio { get; private set; }

    public string? ProfilePictureUrl { get; private set; }

    public Guid? AvatarAssetId { get; private set; }

    public ProfileVisibility? Visibility { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? OnboardingCompletedAt { get; private set; }

    public static Profile Create(
        Guid id,
        Guid accountId,
        string handle,
        string displayName,
        DateTimeOffset createdAt)
    {
        var trimmedHandle = NormalizeRequired(handle, HandleMaxLength, nameof(handle));
        var trimmedDisplayName = NormalizeRequired(displayName, DisplayNameMaxLength, nameof(displayName));

        return new Profile(
            id,
            accountId,
            trimmedHandle,
            trimmedDisplayName,
            createdAt);
    }

    public void CompleteOnboarding(
        string? bio,
        string? profilePictureUrl,
        DateTimeOffset completedAt)
    {
        Bio = NormalizeOptional(bio, BioMaxLength, nameof(bio));
        ProfilePictureUrl = NormalizeOptional(
            profilePictureUrl,
            ProfilePictureUrlMaxLength,
            nameof(profilePictureUrl));
        OnboardingCompletedAt ??= completedAt;
        UpdatedAt = completedAt;
    }

    public void UpdateDetails(
        string handle,
        string displayName,
        string? bio,
        Guid? avatarAssetId,
        DateTimeOffset updatedAt)
    {
        var updatedHandle = NormalizeRequired(handle, HandleMaxLength, nameof(handle));
        var updatedNormalizedHandle = NormalizeHandle(updatedHandle);
        var updatedDisplayName = NormalizeRequired(displayName, DisplayNameMaxLength, nameof(displayName));
        var updatedBio = NormalizeOptional(bio, BioMaxLength, nameof(bio));

        Handle = updatedHandle;
        NormalizedHandle = updatedNormalizedHandle;
        DisplayName = updatedDisplayName;
        Bio = updatedBio;
        AvatarAssetId = avatarAssetId;
        ProfilePictureUrl = null;
        UpdatedAt = updatedAt;
    }

    public void SetVisibility(ProfileVisibility visibility, DateTimeOffset updatedAt)
    {
        if (!Enum.IsDefined(visibility))
        {
            throw new ArgumentOutOfRangeException(nameof(visibility));
        }

        Visibility = visibility;
        UpdatedAt = updatedAt;
    }

    public void MarkOnboardingCompleted(DateTimeOffset completedAt)
    {
        if (Visibility is null)
        {
            throw new InvalidOperationException("Profile visibility must be selected before onboarding completion.");
        }

        OnboardingCompletedAt ??= completedAt;
        UpdatedAt = completedAt;
    }

    public static string NormalizeHandle(string handle)
    {
        var normalized = NormalizeRequired(handle, HandleMaxLength, nameof(handle)).ToLowerInvariant();

        if (normalized.Length < HandleMinLength ||
            normalized[0] is '.' or '_' ||
            normalized[^1] is '.' or '_' ||
            normalized.Any(character => !(character is >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '_')))
        {
            throw new ArgumentException(
                $"Handle must contain {HandleMinLength}-{HandleMaxLength} lowercase letters, numbers, dots or underscores.",
                nameof(handle));
        }

        if (ReservedHandles.Contains(normalized))
        {
            throw new ArgumentException("Handle is reserved.", nameof(handle));
        }

        return normalized;
    }

    private static readonly HashSet<string> ReservedHandles = new(StringComparer.Ordinal)
    {
        "admin", "api", "auth", "help", "kinxter", "legal", "moderation", "settings", "support"
    };

    private static string? NormalizeOptional(string? value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value cannot be longer than {maxLength} characters.",
                parameterName);
        }

        return trimmed;
    }

    private static string NormalizeRequired(string value, int maxLength, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        var trimmed = value.Trim();

        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value cannot be longer than {maxLength} characters.",
                parameterName);
        }

        return trimmed;
    }
}
