namespace Kinxter.Accounts.Model;

public sealed class Account
{
    private Account()
    {
        Email = null!;
        NormalizedEmail = null!;
        IdentityProvider = null!;
        IdentitySubject = null!;
    }

    private Account(
        Guid id,
        string email,
        string identityProvider,
        string identitySubject,
        AccountStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset? emailVerifiedAt)
    {
        Id = id;
        Email = email;
        NormalizedEmail = NormalizeEmail(email);
        IdentityProvider = identityProvider;
        IdentitySubject = identitySubject;
        Status = status;
        CreatedAt = createdAt;
        EmailVerifiedAt = emailVerifiedAt;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; }

    public string NormalizedEmail { get; private set; }

    public string IdentityProvider { get; private set; }

    public string IdentitySubject { get; private set; }

    public AccountStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? EmailVerifiedAt { get; private set; }

    public DateTimeOffset? DisabledAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Account Create(
        Guid id,
        string email,
        string identityProvider,
        string identitySubject,
        bool emailVerified,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(identityProvider);
        ArgumentException.ThrowIfNullOrWhiteSpace(identitySubject);

        var status = emailVerified
            ? AccountStatus.Active
            : AccountStatus.PendingEmailVerification;

        return new Account(
            id,
            email.Trim(),
            identityProvider.Trim(),
            identitySubject.Trim(),
            status,
            createdAt,
            emailVerified ? createdAt : null);
    }

    public void ChangeEmail(string email, bool emailVerified, DateTimeOffset changedAt)
    {
        EnsureNotDeleted();
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        Email = email.Trim();
        NormalizedEmail = NormalizeEmail(email);
        EmailVerifiedAt = emailVerified ? changedAt : null;
        UpdatedAt = changedAt;

        if (Status is AccountStatus.Active or AccountStatus.PendingEmailVerification)
        {
            Status = emailVerified
                ? AccountStatus.Active
                : AccountStatus.PendingEmailVerification;
        }
    }

    public void MarkEmailAsVerified(DateTimeOffset verifiedAt)
    {
        EnsureNotDeleted();

        EmailVerifiedAt ??= verifiedAt;

        if (Status == AccountStatus.PendingEmailVerification)
        {
            Status = AccountStatus.Active;
            UpdatedAt = verifiedAt;
        }
    }

    public void Disable(DateTimeOffset disabledAt)
    {
        EnsureNotDeleted();

        if (Status == AccountStatus.Disabled)
        {
            return;
        }

        Status = AccountStatus.Disabled;
        DisabledAt = disabledAt;
        UpdatedAt = disabledAt;
    }

    public void Delete(DateTimeOffset deletedAt)
    {
        if (Status == AccountStatus.Deleted)
        {
            return;
        }

        Status = AccountStatus.Deleted;
        DeletedAt = deletedAt;
        UpdatedAt = deletedAt;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private void EnsureNotDeleted()
    {
        if (Status == AccountStatus.Deleted)
        {
            throw new InvalidOperationException("Deleted account cannot be changed.");
        }
    }
}
