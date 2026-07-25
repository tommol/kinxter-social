namespace Kinxter.Profiles.Model;

public sealed class Profile
{
    public const int HandleMaxLength = 64;
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

    public static string NormalizeHandle(string handle)
    {
        return NormalizeRequired(handle, HandleMaxLength, nameof(handle)).ToLowerInvariant();
    }

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
