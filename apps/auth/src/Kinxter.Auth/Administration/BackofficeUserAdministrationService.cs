using Kinxter.Auth.Infrastructure.Persistence;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Kinxter.Auth.Administration;

internal sealed class BackofficeUserAdministrationService
{
    private readonly AuthDbContext dbContext;
    private readonly UserManager<AuthUser> userManager;
    private readonly IOpenIddictAuthorizationManager authorizationManager;
    private readonly IOpenIddictTokenManager tokenManager;
    private readonly IClock clock;

    public BackofficeUserAdministrationService(
        AuthDbContext dbContext,
        UserManager<AuthUser> userManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictTokenManager tokenManager,
        IClock clock)
    {
        this.dbContext = dbContext;
        this.userManager = userManager;
        this.authorizationManager = authorizationManager;
        this.tokenManager = tokenManager;
        this.clock = clock;
    }

    public async Task<AuthAdminBackofficeUsers?> GetUsersAsync(
        Guid realmId,
        CancellationToken cancellationToken = default)
    {
        var realm = await FindBackofficeRealmAsync(realmId, cancellationToken);

        if (realm is null)
        {
            return null;
        }

        var users = await this.dbContext.Users
            .AsNoTracking()
            .Where(user => user.Realm == realm.Name && user.DeletedAt == null)
            .OrderBy(user => user.NormalizedEmail)
            .ToArrayAsync(cancellationToken);
        var summaries = new List<AuthAdminBackofficeUserSummary>(users.Length);

        foreach (var user in users)
        {
            var roles = await this.userManager.GetRolesAsync(user);
            summaries.Add(ToSummary(user, roles));
        }

        return new AuthAdminBackofficeUsers(
            realm.Id,
            realm.Name,
            realm.Issuer,
            summaries,
            AuthRoles.Assignable);
    }

    public async Task<AuthAdminBackofficeUserDetails?> GetUserAsync(
        Guid realmId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var realm = await FindBackofficeRealmAsync(realmId, cancellationToken);

        if (realm is null)
        {
            return null;
        }

        var user = await this.dbContext.Users
            .SingleOrDefaultAsync(
                current => current.Id == userId &&
                    current.Realm == realm.Name &&
                    current.DeletedAt == null,
                cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roles = await this.userManager.GetRolesAsync(user);

        return ToDetails(realm, user, roles);
    }

    public async Task<AuthAdminInviteUserResult> InviteAsync(
        Guid realmId,
        AuthAdminInviteUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var realm = await FindBackofficeRealmAsync(realmId, cancellationToken);

        if (realm is null)
        {
            return AuthAdminInviteUserResult.NotFound();
        }

        var email = command.Email.Trim();
        var roles = ValidateRoles(command.Roles, out var roleError);

        if (string.IsNullOrWhiteSpace(email))
        {
            return AuthAdminInviteUserResult.Failed("Email is required.");
        }

        if (roleError is not null)
        {
            return AuthAdminInviteUserResult.Failed(roleError);
        }

        var normalizedEmail = this.userManager.NormalizeEmail(email);
        var alreadyExists = await this.dbContext.Users.AnyAsync(
            user => user.Realm == realm.Name && user.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (alreadyExists)
        {
            return AuthAdminInviteUserResult.Failed("A user with this email already exists in the backoffice realm.");
        }

        var user = new AuthUser
        {
            Id = Guid.CreateVersion7(this.clock.UtcNow),
            Realm = realm.Name,
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            CreatedAt = this.clock.UtcNow
        };
        var createResult = await this.userManager.CreateAsync(user);

        if (!createResult.Succeeded)
        {
            return AuthAdminInviteUserResult.Failed(FormatErrors(createResult));
        }

        var roleResult = await this.userManager.AddToRolesAsync(user, roles);

        if (!roleResult.Succeeded)
        {
            await this.userManager.DeleteAsync(user);
            return AuthAdminInviteUserResult.Failed(FormatErrors(roleResult));
        }

        var token = await this.userManager.GeneratePasswordResetTokenAsync(user);
        var invitationUrl = BuildInvitationUrl(realm.Issuer, email, token);

        return AuthAdminInviteUserResult.Succeeded(
            ToDetails(realm, user, roles),
            invitationUrl);
    }

    public async Task<AuthAdminUserActionResult> UpdateRolesAsync(
        Guid realmId,
        Guid userId,
        IReadOnlyCollection<string> requestedRoles,
        CancellationToken cancellationToken = default)
    {
        var loaded = await FindUserAsync(realmId, userId, cancellationToken);

        if (loaded is null)
        {
            return AuthAdminUserActionResult.NotFound();
        }

        var roles = ValidateRoles(requestedRoles, out var roleError);

        if (roleError is not null)
        {
            return AuthAdminUserActionResult.Failed(roleError);
        }

        var currentRoles = await this.userManager.GetRolesAsync(loaded.Value.User);
        var removableRoles = currentRoles
            .Where(role => AuthRoles.AllNames.Contains(role, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (removableRoles.Length > 0)
        {
            var removeResult = await this.userManager.RemoveFromRolesAsync(loaded.Value.User, removableRoles);

            if (!removeResult.Succeeded)
            {
                return AuthAdminUserActionResult.Failed(FormatErrors(removeResult));
            }
        }

        var addResult = await this.userManager.AddToRolesAsync(loaded.Value.User, roles);

        if (!addResult.Succeeded)
        {
            return AuthAdminUserActionResult.Failed(FormatErrors(addResult));
        }

        await RevokeSessionsAsync(loaded.Value.User, cancellationToken);

        return AuthAdminUserActionResult.Succeeded();
    }

    public async Task<AuthAdminUserActionResult> SetEnabledAsync(
        Guid realmId,
        Guid userId,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        var loaded = await FindUserAsync(realmId, userId, cancellationToken);

        if (loaded is null)
        {
            return AuthAdminUserActionResult.NotFound();
        }

        loaded.Value.User.DisabledAt = enabled ? null : this.clock.UtcNow;
        var updateResult = await this.userManager.UpdateAsync(loaded.Value.User);

        if (!updateResult.Succeeded)
        {
            return AuthAdminUserActionResult.Failed(FormatErrors(updateResult));
        }

        await RevokeSessionsAsync(loaded.Value.User, cancellationToken);

        return AuthAdminUserActionResult.Succeeded();
    }

    public async Task<AuthAdminUserActionResult> RevokeSessionsAsync(
        Guid realmId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var loaded = await FindUserAsync(realmId, userId, cancellationToken);

        if (loaded is null)
        {
            return AuthAdminUserActionResult.NotFound();
        }

        await RevokeSessionsAsync(loaded.Value.User, cancellationToken);

        return AuthAdminUserActionResult.Succeeded();
    }

    public async Task<AuthAdminUserActionResult> ResetMfaAsync(
        Guid realmId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var loaded = await FindUserAsync(realmId, userId, cancellationToken);

        if (loaded is null)
        {
            return AuthAdminUserActionResult.NotFound();
        }

        var disableResult = await this.userManager.SetTwoFactorEnabledAsync(loaded.Value.User, false);

        if (!disableResult.Succeeded)
        {
            return AuthAdminUserActionResult.Failed(FormatErrors(disableResult));
        }

        var resetResult = await this.userManager.ResetAuthenticatorKeyAsync(loaded.Value.User);

        if (!resetResult.Succeeded)
        {
            return AuthAdminUserActionResult.Failed(FormatErrors(resetResult));
        }

        await RevokeSessionsAsync(loaded.Value.User, cancellationToken);

        return AuthAdminUserActionResult.Succeeded();
    }

    public async Task<AuthAdminInviteUserResult> RenewInvitationAsync(
        Guid realmId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var loaded = await FindUserAsync(realmId, userId, cancellationToken);

        if (loaded is null)
        {
            return AuthAdminInviteUserResult.NotFound();
        }

        var user = loaded.Value.User;

        if (await this.userManager.HasPasswordAsync(user))
        {
            return AuthAdminInviteUserResult.Failed("This user has already accepted the invitation.");
        }

        var token = await this.userManager.GeneratePasswordResetTokenAsync(user);
        var roles = await this.userManager.GetRolesAsync(user);

        return AuthAdminInviteUserResult.Succeeded(
            ToDetails(loaded.Value.Realm, user, roles),
            BuildInvitationUrl(loaded.Value.Realm.Issuer, user.Email!, token));
    }

    private async Task RevokeSessionsAsync(AuthUser user, CancellationToken cancellationToken)
    {
        var securityStampResult = await this.userManager.UpdateSecurityStampAsync(user);

        if (!securityStampResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"The security stamp for user '{user.Id:D}' could not be updated: {FormatErrors(securityStampResult)}");
        }

        var subject = user.Id.ToString("D");

        await foreach (var token in this.tokenManager.FindBySubjectAsync(subject, cancellationToken))
        {
            await this.tokenManager.TryRevokeAsync(token, cancellationToken);
        }

        await foreach (var authorization in this.authorizationManager.FindBySubjectAsync(subject, cancellationToken))
        {
            await this.authorizationManager.TryRevokeAsync(authorization, cancellationToken);
        }
    }

    private async Task<AuthRealm?> FindBackofficeRealmAsync(
        Guid realmId,
        CancellationToken cancellationToken)
    {
        return await this.dbContext.AuthRealms
            .AsNoTracking()
            .SingleOrDefaultAsync(
                realm => realm.Id == realmId && realm.Name == AuthRealmNames.Backoffice,
                cancellationToken);
    }

    private async Task<(AuthRealm Realm, AuthUser User)?> FindUserAsync(
        Guid realmId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var realm = await FindBackofficeRealmAsync(realmId, cancellationToken);

        if (realm is null)
        {
            return null;
        }

        var user = await this.dbContext.Users.SingleOrDefaultAsync(
            current => current.Id == userId &&
                current.Realm == realm.Name &&
                current.DeletedAt == null,
            cancellationToken);

        return user is null ? null : (realm, user);
    }

    private static string[] ValidateRoles(
        IEnumerable<string> requestedRoles,
        out string? error)
    {
        var roles = requestedRoles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (roles.Length == 0)
        {
            error = "Select at least one role.";
            return [];
        }

        var invalidRole = roles.FirstOrDefault(role => !AuthRoles.IsAssignable(role));

        if (invalidRole is not null)
        {
            error = $"Role '{invalidRole}' cannot be assigned.";
            return [];
        }

        error = null;
        return roles;
    }

    private static string BuildInvitationUrl(string issuer, string email, string token)
    {
        return $"{issuer.TrimEnd('/')}/account/activate" +
            $"?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
    }

    private static string FormatErrors(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(error => error.Description));

    private static AuthAdminBackofficeUserSummary ToSummary(
        AuthUser user,
        IEnumerable<string> roles)
    {
        return new AuthAdminBackofficeUserSummary(
            user.Id,
            user.Email ?? user.UserName ?? user.Id.ToString("D"),
            user.DisabledAt is null,
            string.IsNullOrWhiteSpace(user.PasswordHash),
            user.TwoFactorEnabled,
            roles.OrderBy(role => role, StringComparer.Ordinal).ToArray(),
            user.CreatedAt,
            user.DisabledAt);
    }

    private static AuthAdminBackofficeUserDetails ToDetails(
        AuthRealm realm,
        AuthUser user,
        IEnumerable<string> roles)
    {
        return new AuthAdminBackofficeUserDetails(
            realm.Id,
            realm.Name,
            user.Id,
            user.Email ?? user.UserName ?? user.Id.ToString("D"),
            user.DisabledAt is null,
            string.IsNullOrWhiteSpace(user.PasswordHash),
            user.EmailConfirmed,
            user.TwoFactorEnabled,
            roles.OrderBy(role => role, StringComparer.Ordinal).ToArray(),
            user.CreatedAt,
            user.DisabledAt,
            AuthRoles.Assignable);
    }
}

internal sealed record AuthAdminBackofficeUsers(
    Guid RealmId,
    string RealmName,
    string Issuer,
    IReadOnlyList<AuthAdminBackofficeUserSummary> Users,
    IReadOnlyList<AuthRoleDefinition> AvailableRoles);

internal sealed record AuthAdminBackofficeUserSummary(
    Guid Id,
    string Email,
    bool Enabled,
    bool InvitationPending,
    bool MfaEnabled,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DisabledAt);

internal sealed record AuthAdminBackofficeUserDetails(
    Guid RealmId,
    string RealmName,
    Guid UserId,
    string Email,
    bool Enabled,
    bool InvitationPending,
    bool EmailConfirmed,
    bool MfaEnabled,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DisabledAt,
    IReadOnlyList<AuthRoleDefinition> AvailableRoles);

internal sealed record AuthAdminInviteUserCommand(
    string Email,
    IReadOnlyCollection<string> Roles);

internal sealed record AuthAdminInviteUserResult(
    bool Success,
    bool UserNotFound,
    string? Error,
    AuthAdminBackofficeUserDetails? User,
    string? InvitationUrl)
{
    public static AuthAdminInviteUserResult Succeeded(
        AuthAdminBackofficeUserDetails user,
        string invitationUrl) =>
        new(true, false, null, user, invitationUrl);

    public static AuthAdminInviteUserResult Failed(string error) =>
        new(false, false, error, null, null);

    public static AuthAdminInviteUserResult NotFound() =>
        new(false, true, null, null, null);
}

internal sealed record AuthAdminUserActionResult(
    bool Success,
    bool UserNotFound,
    string? Error)
{
    public static AuthAdminUserActionResult Succeeded() => new(true, false, null);

    public static AuthAdminUserActionResult Failed(string error) => new(false, false, error);

    public static AuthAdminUserActionResult NotFound() => new(false, true, null);
}
