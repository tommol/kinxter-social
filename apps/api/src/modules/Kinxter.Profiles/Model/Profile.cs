namespace Kinxter.Profiles.Model;

public sealed class Profile
{
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

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Profile Create(
        Guid id,
        Guid accountId,
        string handle,
        string displayName,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handle);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new Profile(
            id,
            accountId,
            handle.Trim(),
            displayName.Trim(),
            createdAt);
    }

    private static string NormalizeHandle(string handle) => handle.Trim().ToLowerInvariant();
}
