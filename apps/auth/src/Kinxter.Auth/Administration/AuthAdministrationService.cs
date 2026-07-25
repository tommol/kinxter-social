using System.Text.RegularExpressions;
using Kinxter.Auth.Infrastructure.Persistence;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Auth.Administration;

internal sealed class AuthAdministrationService
{
    private static readonly Regex RealmPathPattern = new(
        "^/realms/[a-z0-9][a-z0-9._/-]{0,127}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly AuthDbContext dbContext;
    private readonly AuthRealmRegistry realmRegistry;
    private readonly IClock clock;

    public AuthAdministrationService(
        AuthDbContext dbContext,
        AuthRealmRegistry realmRegistry,
        IClock clock)
    {
        this.dbContext = dbContext;
        this.realmRegistry = realmRegistry;
        this.clock = clock;
    }

    public async Task<IReadOnlyList<AuthAdminRealmSummary>> GetRealmsAsync(
        CancellationToken cancellationToken = default)
    {
        var userCounts = await this.dbContext.Users
            .AsNoTracking()
            .Where(user => user.DeletedAt == null)
            .GroupBy(user => user.Realm)
            .Select(group => new
            {
                Realm = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(
                item => item.Realm,
                item => item.Count,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);

        var realms = await this.dbContext.AuthRealms
            .AsNoTracking()
            .Include(realm => realm.Clients)
            .OrderBy(realm => realm.Name)
            .ToArrayAsync(cancellationToken);

        return realms
            .Select(realm => new AuthAdminRealmSummary(
                realm.Id,
                realm.Name,
                realm.Issuer,
                realm.PathBase,
                realm.MfaPolicy,
                realm.SignupEnabled,
                realm.Clients.Count(client => client.Enabled),
                userCounts.GetValueOrDefault(realm.Name),
                realm.UpdatedAt))
            .ToArray();
    }

    public async Task<AuthAdminRealmDetails?> GetRealmAsync(
        Guid realmId,
        CancellationToken cancellationToken = default)
    {
        return await this.dbContext.AuthRealms
            .AsNoTracking()
            .Where(realm => realm.Id == realmId)
            .Select(realm => new AuthAdminRealmDetails(
                realm.Id,
                realm.Name,
                realm.Issuer,
                realm.PathBase,
                realm.MfaPolicy,
                realm.SignupEnabled,
                realm.CreatedAt,
                realm.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<AuthAdminUpdateRealmResult> UpdateRealmAsync(
        Guid realmId,
        AuthAdminUpdateRealmCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var issuer = command.Issuer.Trim().TrimEnd('/');
        var pathBase = NormalizePathBase(command.PathBase);
        var validationError = Validate(issuer, pathBase);

        if (validationError is not null)
        {
            return AuthAdminUpdateRealmResult.Failed(validationError);
        }

        var realm = await this.dbContext.AuthRealms
            .SingleOrDefaultAsync(current => current.Id == realmId, cancellationToken);

        if (realm is null)
        {
            return AuthAdminUpdateRealmResult.NotFound();
        }

        var otherPaths = await this.dbContext.AuthRealms
            .AsNoTracking()
            .Where(current => current.Id != realmId)
            .Select(current => current.PathBase)
            .ToArrayAsync(cancellationToken);
        var pathIsUsed = otherPaths.Any(otherPath =>
            string.Equals(otherPath, pathBase, StringComparison.OrdinalIgnoreCase));

        if (pathIsUsed)
        {
            return AuthAdminUpdateRealmResult.Failed(
                $"Path base '{pathBase}' is already used by another realm.");
        }

        realm.Issuer = issuer;
        realm.PathBase = pathBase;
        realm.MfaPolicy = command.MfaPolicy;
        realm.SignupEnabled = command.SignupEnabled;
        realm.UpdatedAt = this.clock.UtcNow;

        await this.dbContext.SaveChangesAsync(cancellationToken);
        this.realmRegistry.Update(realm);

        return AuthAdminUpdateRealmResult.Succeeded(realm);
    }

    private static string? Validate(string issuer, string pathBase)
    {
        if (!Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri) ||
            (issuerUri.Scheme != Uri.UriSchemeHttp && issuerUri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(issuerUri.Query) ||
            !string.IsNullOrEmpty(issuerUri.Fragment))
        {
            return "Issuer must be an absolute HTTP or HTTPS URL without a query or fragment.";
        }

        if (!RealmPathPattern.IsMatch(pathBase) ||
            pathBase.Contains("//", StringComparison.Ordinal))
        {
            return "Path base must start with '/realms/' and contain only URL-safe path characters.";
        }

        if (!string.Equals(
                issuerUri.AbsolutePath.TrimEnd('/'),
                pathBase,
                StringComparison.OrdinalIgnoreCase))
        {
            return "The issuer URL path must match the realm path base.";
        }

        return null;
    }

    private static string NormalizePathBase(string pathBase)
    {
        var normalized = pathBase.Trim().TrimEnd('/');

        return normalized.StartsWith("/", StringComparison.Ordinal)
            ? normalized
            : $"/{normalized}";
    }
}

internal sealed record AuthAdminRealmSummary(
    Guid Id,
    string Name,
    string Issuer,
    string PathBase,
    AuthMfaPolicy MfaPolicy,
    bool SignupEnabled,
    int ActiveClientCount,
    int ActiveUserCount,
    DateTimeOffset? UpdatedAt);

internal sealed record AuthAdminRealmDetails(
    Guid Id,
    string Name,
    string Issuer,
    string PathBase,
    AuthMfaPolicy MfaPolicy,
    bool SignupEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

internal sealed record AuthAdminUpdateRealmCommand(
    string Issuer,
    string PathBase,
    AuthMfaPolicy MfaPolicy,
    bool SignupEnabled);

internal sealed record AuthAdminUpdateRealmResult(
    bool Success,
    bool RealmNotFound,
    string? Error,
    AuthRealm? Realm)
{
    public static AuthAdminUpdateRealmResult Succeeded(AuthRealm realm) =>
        new(true, false, null, realm);

    public static AuthAdminUpdateRealmResult Failed(string error) =>
        new(false, false, error, null);

    public static AuthAdminUpdateRealmResult NotFound() =>
        new(false, true, null, null);
}
