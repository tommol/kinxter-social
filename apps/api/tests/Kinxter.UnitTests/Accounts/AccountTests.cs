using Kinxter.Accounts.Model;
using Xunit;

namespace Kinxter.UnitTests.Accounts;

public sealed class AccountTests
{
    [Fact]
    public void Create_sets_active_status_when_email_is_verified()
    {
        var now = DateTimeOffset.UtcNow;

        var account = Account.Create(
            Guid.CreateVersion7(now),
            " User@Example.com ",
            "identity-provider",
            "subject-123",
            emailVerified: true,
            now);

        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Equal("User@Example.com", account.Email);
        Assert.Equal("user@example.com", account.NormalizedEmail);
        Assert.Equal(now, account.EmailVerifiedAt);
    }

    [Fact]
    public void Create_sets_pending_status_when_email_is_not_verified()
    {
        var now = DateTimeOffset.UtcNow;

        var account = Account.Create(
            Guid.CreateVersion7(now),
            "user@example.com",
            "identity-provider",
            "subject-123",
            emailVerified: false,
            now);

        Assert.Equal(AccountStatus.PendingEmailVerification, account.Status);
        Assert.Null(account.EmailVerifiedAt);
    }
}
