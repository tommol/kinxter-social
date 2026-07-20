using System.Security.Claims;
using Kinxter.Auth.Infrastructure.Persistence;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;

namespace Kinxter.Auth;

internal sealed class ExternalLoginAccountManager
{
    private readonly UserManager<AuthUser> userManager;
    private readonly AuthDbContext dbContext;
    private readonly AuthIntegrationEventPublisher eventPublisher;
    private readonly AuthOptions options;
    private readonly IClock clock;

    public ExternalLoginAccountManager(
        UserManager<AuthUser> userManager,
        AuthDbContext dbContext,
        AuthIntegrationEventPublisher eventPublisher,
        AuthOptions options,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(eventPublisher);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);

        this.userManager = userManager;
        this.dbContext = dbContext;
        this.eventPublisher = eventPublisher;
        this.options = options;
        this.clock = clock;
    }

    public async Task<ExternalLoginAccountResult> ResolveForSignInAsync(
        ExternalLoginInfo login,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(login);

        var linkedUser = await this.userManager.FindByLoginAsync(login.LoginProvider, login.ProviderKey);

        if (linkedUser is not null)
        {
            return IsActiveRealmUser(linkedUser)
                ? ExternalLoginAccountResult.Success(ExternalLoginAccountStatus.ExistingLinkedUser, linkedUser)
                : ExternalLoginAccountResult.Failure(ExternalLoginAccountStatus.UserUnavailable);
        }

        var email = GetVerifiedEmail(login.Principal);

        if (email is null)
        {
            return ExternalLoginAccountResult.Failure(ExternalLoginAccountStatus.EmailNotVerified);
        }

        var existingUserWithEmail = await this.userManager.FindByEmailInRealmAsync(
            this.dbContext,
            this.options,
            email,
            cancellationToken);

        if (existingUserWithEmail is not null)
        {
            return ExternalLoginAccountResult.Failure(ExternalLoginAccountStatus.EmailAlreadyExists);
        }

        var now = this.clock.UtcNow;
        var user = new AuthUser
        {
            Id = Guid.CreateVersion7(now),
            Realm = this.options.Realm,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            CreatedAt = now
        };
        var createResult = await this.userManager.CreateAsync(user);

        if (!createResult.Succeeded)
        {
            return ExternalLoginAccountResult.IdentityFailure(
                ExternalLoginAccountStatus.IdentityError,
                createResult);
        }

        var addLoginResult = await this.userManager.AddLoginAsync(user, login);

        if (!addLoginResult.Succeeded)
        {
            return ExternalLoginAccountResult.IdentityFailure(
                ExternalLoginAccountStatus.IdentityError,
                addLoginResult);
        }

        await this.eventPublisher.PublishUserRegisteredAsync(user, cancellationToken);

        return ExternalLoginAccountResult.Success(ExternalLoginAccountStatus.CreatedUser, user);
    }

    public async Task<ExternalLoginLinkResult> LinkAsync(
        AuthUser user,
        ExternalLoginInfo login)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(login);

        if (!IsActiveRealmUser(user))
        {
            return ExternalLoginLinkResult.Failure(ExternalLoginLinkStatus.UserUnavailable);
        }

        var linkedUser = await this.userManager.FindByLoginAsync(login.LoginProvider, login.ProviderKey);

        if (linkedUser is not null)
        {
            return linkedUser.Id == user.Id
                ? ExternalLoginLinkResult.Success(ExternalLoginLinkStatus.AlreadyLinked)
                : ExternalLoginLinkResult.Failure(ExternalLoginLinkStatus.LinkedToAnotherUser);
        }

        var result = await this.userManager.AddLoginAsync(user, login);

        return result.Succeeded
            ? ExternalLoginLinkResult.Success(ExternalLoginLinkStatus.Linked)
            : ExternalLoginLinkResult.IdentityFailure(result);
    }

    private bool IsActiveRealmUser(AuthUser user)
    {
        return user.Realm == this.options.Realm &&
            user.DeletedAt is null &&
            user.DisabledAt is null;
    }

    private static string? GetVerifiedEmail(ClaimsPrincipal principal)
    {
        var email = principal.FindFirstValue(OpenIddictConstants.Claims.Email) ??
            principal.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(email) || !HasVerifiedEmail(principal))
        {
            return null;
        }

        return email.Trim();
    }

    private static bool HasVerifiedEmail(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("email_verified") ??
            principal.FindFirstValue("verified_email");

        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed record ExternalLoginAccountResult(
    ExternalLoginAccountStatus Status,
    AuthUser? User,
    string? Error)
{
    public static ExternalLoginAccountResult Success(ExternalLoginAccountStatus status, AuthUser user)
    {
        return new ExternalLoginAccountResult(status, user, null);
    }

    public static ExternalLoginAccountResult Failure(ExternalLoginAccountStatus status)
    {
        return new ExternalLoginAccountResult(status, null, null);
    }

    public static ExternalLoginAccountResult IdentityFailure(
        ExternalLoginAccountStatus status,
        IdentityResult result)
    {
        return new ExternalLoginAccountResult(status, null, FormatIdentityErrors(result));
    }

    private static string FormatIdentityErrors(IdentityResult result)
    {
        return string.Join(" ", result.Errors.Select(error => error.Description));
    }
}

internal enum ExternalLoginAccountStatus
{
    ExistingLinkedUser = 1,
    CreatedUser = 2,
    UserUnavailable = 3,
    EmailNotVerified = 4,
    EmailAlreadyExists = 5,
    IdentityError = 6
}

internal sealed record ExternalLoginLinkResult(
    ExternalLoginLinkStatus Status,
    string? Error)
{
    public static ExternalLoginLinkResult Success(ExternalLoginLinkStatus status)
    {
        return new ExternalLoginLinkResult(status, null);
    }

    public static ExternalLoginLinkResult Failure(ExternalLoginLinkStatus status)
    {
        return new ExternalLoginLinkResult(status, null);
    }

    public static ExternalLoginLinkResult IdentityFailure(IdentityResult result)
    {
        return new ExternalLoginLinkResult(
            ExternalLoginLinkStatus.IdentityError,
            string.Join(" ", result.Errors.Select(error => error.Description)));
    }
}

internal enum ExternalLoginLinkStatus
{
    Linked = 1,
    AlreadyLinked = 2,
    LinkedToAnotherUser = 3,
    UserUnavailable = 4,
    IdentityError = 5
}
