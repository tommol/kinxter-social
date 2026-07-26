using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Kinxter.Auth.Infrastructure.Persistence;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Kinxter.Auth.Administration;

internal sealed class AuthClientAdministrationService
{
    private static readonly Regex ClientIdPattern = new(
        "^[A-Za-z0-9][A-Za-z0-9._~-]{2,99}$",
        RegexOptions.Compiled);

    private static readonly HashSet<string> SupportedScopes = new(
        [
            Scopes.OpenId,
            Scopes.Profile,
            Scopes.Email,
            Scopes.Roles,
            Scopes.OfflineAccess,
            AuthScopes.KinxterApi,
            AuthScopes.KinxterAdmin
        ],
        StringComparer.Ordinal);

    private static readonly HashSet<string> SupportedGrantTypes = new(
        [
            AuthClientGrantTypes.AuthorizationCode,
            AuthClientGrantTypes.RefreshToken,
            AuthClientGrantTypes.ClientCredentials,
            AuthClientGrantTypes.DeviceCode
        ],
        StringComparer.Ordinal);

    private readonly AuthDbContext dbContext;
    private readonly IOpenIddictApplicationManager applicationManager;
    private readonly IClock clock;

    public AuthClientAdministrationService(
        AuthDbContext dbContext,
        IOpenIddictApplicationManager applicationManager,
        IClock clock)
    {
        this.dbContext = dbContext;
        this.applicationManager = applicationManager;
        this.clock = clock;
    }

    public async Task<AuthAdminClientDetails?> GetClientAsync(
        Guid realmId,
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        return await this.dbContext.AuthClients
            .AsNoTracking()
            .Where(client => client.RealmId == realmId && client.Id == clientId)
            .Select(client => new AuthAdminClientDetails(
                client.Id,
                client.RealmId,
                client.Realm.Name,
                client.ClientId,
                client.DisplayName,
                client.Enabled,
                client.ClientSecretConfigured,
                client.ClientType,
                client.GrantTypes,
                client.RedirectUris,
                client.PostLogoutRedirectUris,
                client.Scopes,
                client.CreatedAt,
                client.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<AuthAdminCreateClientResult> CreateClientAsync(
        Guid realmId,
        AuthAdminCreateClientCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var normalized = Normalize(command);
        var validationError = Validate(normalized);

        if (validationError is not null)
        {
            return AuthAdminCreateClientResult.Failed(validationError);
        }

        var realm = await this.dbContext.AuthRealms
            .SingleOrDefaultAsync(current => current.Id == realmId, cancellationToken);

        if (realm is null)
        {
            return AuthAdminCreateClientResult.NotFound();
        }

        if (await this.dbContext.AuthClients.AnyAsync(
                current => current.ClientId == normalized.ClientId,
                cancellationToken) ||
            await this.applicationManager.FindByClientIdAsync(normalized.ClientId, cancellationToken) is not null)
        {
            return AuthAdminCreateClientResult.Failed(
                $"Client ID '{normalized.ClientId}' is already registered.");
        }

        var now = this.clock.UtcNow;
        var secret = normalized.ClientType == AuthClientType.Confidential
            ? GenerateClientSecret()
            : null;
        var client = new AuthClient
        {
            Id = Guid.CreateVersion7(now),
            RealmId = realm.Id,
            ClientId = normalized.ClientId,
            DisplayName = normalized.DisplayName,
            Enabled = true,
            ClientType = normalized.ClientType,
            GrantTypes = normalized.GrantTypes,
            ClientSecretConfigured = secret is not null,
            RedirectUris = normalized.RedirectUris,
            PostLogoutRedirectUris = normalized.PostLogoutRedirectUris,
            Scopes = normalized.Scopes,
            CreatedAt = now
        };

        this.dbContext.AuthClients.Add(client);
        await this.applicationManager.CreateAsync(
            OpenIddictClientDescriptorFactory.Create(client, secret),
            cancellationToken);

        return AuthAdminCreateClientResult.Succeeded(client, realm.Name, secret);
    }

    public async Task<AuthAdminUpdateClientResult> UpdateClientAsync(
        Guid realmId,
        Guid clientId,
        AuthAdminUpdateClientCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var normalized = Normalize(command);
        var validationError = Validate(normalized);

        if (validationError is not null)
        {
            return AuthAdminUpdateClientResult.Failed(validationError);
        }

        var client = await this.dbContext.AuthClients
            .Include(current => current.Realm)
            .SingleOrDefaultAsync(
                current => current.RealmId == realmId && current.Id == clientId,
                cancellationToken);

        if (client is null)
        {
            return AuthAdminUpdateClientResult.NotFound();
        }

        var application = await this.applicationManager.FindByClientIdAsync(
            client.ClientId,
            cancellationToken);

        if (application is null)
        {
            return AuthAdminUpdateClientResult.Failed(
                "The OpenIddict registration is missing. Recreate the client.");
        }

        var clientTypeChanged = client.ClientType != normalized.ClientType;
        var secret = clientTypeChanged && normalized.ClientType == AuthClientType.Confidential
            ? GenerateClientSecret()
            : null;

        client.DisplayName = normalized.DisplayName;
        client.Enabled = normalized.Enabled;
        client.ClientType = normalized.ClientType;
        client.GrantTypes = normalized.GrantTypes;
        client.ClientSecretConfigured = normalized.ClientType == AuthClientType.Confidential &&
            (client.ClientSecretConfigured || secret is not null);
        client.RedirectUris = normalized.RedirectUris;
        client.PostLogoutRedirectUris = normalized.PostLogoutRedirectUris;
        client.Scopes = normalized.Scopes;
        client.UpdatedAt = this.clock.UtcNow;

        var descriptor = new OpenIddictApplicationDescriptor();
        await this.applicationManager.PopulateAsync(descriptor, application, cancellationToken);
        OpenIddictClientDescriptorFactory.ApplyRegistration(descriptor, client);

        if (secret is not null)
        {
            descriptor.ClientSecret = secret;
        }

        if (clientTypeChanged && normalized.ClientType == AuthClientType.Public)
        {
            if (application is not OpenIddictEntityFrameworkCoreApplication entity)
            {
                return AuthAdminUpdateClientResult.Failed(
                    "The client secret cannot be removed from the current OpenIddict store.");
            }

            entity.ClientSecret = null;
            descriptor.ClientSecret = null;
        }

        await this.applicationManager.UpdateAsync(application, descriptor, cancellationToken);

        return AuthAdminUpdateClientResult.Succeeded(client, client.Realm.Name, secret);
    }

    public async Task<AuthAdminRotateClientSecretResult> RotateSecretAsync(
        Guid realmId,
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var client = await this.dbContext.AuthClients
            .Include(current => current.Realm)
            .SingleOrDefaultAsync(
                current => current.RealmId == realmId && current.Id == clientId,
                cancellationToken);

        if (client is null)
        {
            return AuthAdminRotateClientSecretResult.NotFound();
        }

        if (client.ClientType != AuthClientType.Confidential)
        {
            return AuthAdminRotateClientSecretResult.Failed(
                "Public clients do not use a client secret.");
        }

        var application = await this.applicationManager.FindByClientIdAsync(
            client.ClientId,
            cancellationToken);

        if (application is null)
        {
            return AuthAdminRotateClientSecretResult.Failed(
                "The OpenIddict registration is missing. Recreate the client.");
        }

        var secret = GenerateClientSecret();
        client.ClientSecretConfigured = true;
        client.UpdatedAt = this.clock.UtcNow;
        await this.applicationManager.UpdateAsync(application, secret, cancellationToken);

        return AuthAdminRotateClientSecretResult.Succeeded(client, client.Realm.Name, secret);
    }

    private static AuthAdminCreateClientCommand Normalize(AuthAdminCreateClientCommand command)
    {
        return command with
        {
            ClientId = command.ClientId.Trim(),
            DisplayName = command.DisplayName.Trim(),
            GrantTypes = CleanValues(command.GrantTypes),
            RedirectUris = CleanValues(command.RedirectUris),
            PostLogoutRedirectUris = CleanValues(command.PostLogoutRedirectUris),
            Scopes = CleanValues(command.Scopes)
        };
    }

    private static AuthAdminUpdateClientCommand Normalize(AuthAdminUpdateClientCommand command)
    {
        return command with
        {
            DisplayName = command.DisplayName.Trim(),
            GrantTypes = CleanValues(command.GrantTypes),
            RedirectUris = CleanValues(command.RedirectUris),
            PostLogoutRedirectUris = CleanValues(command.PostLogoutRedirectUris),
            Scopes = CleanValues(command.Scopes)
        };
    }

    private static string? Validate(AuthAdminCreateClientCommand command)
    {
        if (!ClientIdPattern.IsMatch(command.ClientId))
        {
            return "Client ID must contain 3-100 letters, digits, dots, underscores, tildes or hyphens.";
        }

        return ValidateRegistration(
            command.DisplayName,
            command.ClientType,
            command.GrantTypes,
            command.RedirectUris,
            command.PostLogoutRedirectUris,
            command.Scopes);
    }

    private static string? Validate(AuthAdminUpdateClientCommand command)
    {
        return ValidateRegistration(
            command.DisplayName,
            command.ClientType,
            command.GrantTypes,
            command.RedirectUris,
            command.PostLogoutRedirectUris,
            command.Scopes);
    }

    private static string? ValidateRegistration(
        string displayName,
        AuthClientType clientType,
        IReadOnlyCollection<string> grantTypes,
        IReadOnlyCollection<string> redirectUris,
        IReadOnlyCollection<string> postLogoutRedirectUris,
        IReadOnlyCollection<string> scopes)
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 200)
        {
            return "Display name must contain 1-200 characters.";
        }

        if (!Enum.IsDefined(clientType))
        {
            return "The client type is not supported.";
        }

        var unsupportedGrantTypes = grantTypes
            .Where(grantType => !SupportedGrantTypes.Contains(grantType))
            .ToArray();

        if (unsupportedGrantTypes.Length > 0)
        {
            return $"Unsupported grant types: {string.Join(", ", unsupportedGrantTypes)}.";
        }

        var hasAuthorizationCode = grantTypes.Contains(
            AuthClientGrantTypes.AuthorizationCode,
            StringComparer.Ordinal);
        var hasDeviceCode = grantTypes.Contains(
            AuthClientGrantTypes.DeviceCode,
            StringComparer.Ordinal);
        var hasClientCredentials = grantTypes.Contains(
            AuthClientGrantTypes.ClientCredentials,
            StringComparer.Ordinal);
        var hasRefreshToken = grantTypes.Contains(
            AuthClientGrantTypes.RefreshToken,
            StringComparer.Ordinal);

        if (!hasAuthorizationCode && !hasDeviceCode && !hasClientCredentials)
        {
            return "Select at least one primary flow: Authorization Code, Device Code or Client Credentials.";
        }

        if (clientType == AuthClientType.Public && hasClientCredentials)
        {
            return "Client Credentials requires a confidential client.";
        }

        if (hasRefreshToken && !hasAuthorizationCode && !hasDeviceCode)
        {
            return "Refresh Token requires Authorization Code or Device Code.";
        }

        if (hasAuthorizationCode && redirectUris.Count == 0)
        {
            return "At least one redirect URI is required for Authorization Code.";
        }

        if (redirectUris.Concat(postLogoutRedirectUris).Any(uri => !IsValidApplicationUri(uri)))
        {
            return "Redirect URIs must be absolute HTTP or HTTPS URLs without fragments.";
        }

        if (scopes.Contains(Scopes.OfflineAccess, StringComparer.Ordinal) && !hasRefreshToken)
        {
            return "The offline_access scope requires the Refresh Token grant.";
        }

        var unsupportedScopes = scopes
            .Where(scope => !SupportedScopes.Contains(scope))
            .ToArray();

        if (unsupportedScopes.Length > 0)
        {
            return $"Unsupported scopes: {string.Join(", ", unsupportedScopes)}.";
        }

        return null;
    }

    private static bool IsValidApplicationUri(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
            string.IsNullOrEmpty(uri.Fragment);
    }

    private static string[] CleanValues(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string GenerateClientSecret()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    }
}

internal sealed record AuthAdminClientSummary(
    Guid Id,
    string ClientId,
    string DisplayName,
    bool Enabled,
    bool ClientSecretConfigured,
    AuthClientType ClientType,
    string[] GrantTypes,
    string[] RedirectUris,
    string[] Scopes,
    DateTimeOffset? UpdatedAt);

internal sealed record AuthAdminClientDetails(
    Guid Id,
    Guid RealmId,
    string RealmName,
    string ClientId,
    string DisplayName,
    bool Enabled,
    bool ClientSecretConfigured,
    AuthClientType ClientType,
    string[] GrantTypes,
    string[] RedirectUris,
    string[] PostLogoutRedirectUris,
    string[] Scopes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

internal sealed record AuthAdminCreateClientCommand(
    string ClientId,
    string DisplayName,
    AuthClientType ClientType,
    string[] GrantTypes,
    string[] RedirectUris,
    string[] PostLogoutRedirectUris,
    string[] Scopes);

internal sealed record AuthAdminUpdateClientCommand(
    string DisplayName,
    bool Enabled,
    AuthClientType ClientType,
    string[] GrantTypes,
    string[] RedirectUris,
    string[] PostLogoutRedirectUris,
    string[] Scopes);

internal sealed record AuthAdminCreateClientResult(
    bool Success,
    bool RealmNotFound,
    string? Error,
    AuthAdminClientDetails? Client,
    string? ClientSecret)
{
    public static AuthAdminCreateClientResult Succeeded(
        AuthClient client,
        string realmName,
        string? clientSecret) =>
        new(true, false, null, ToDetails(client, realmName), clientSecret);

    public static AuthAdminCreateClientResult Failed(string error) =>
        new(false, false, error, null, null);

    public static AuthAdminCreateClientResult NotFound() =>
        new(false, true, null, null, null);

    internal static AuthAdminClientDetails ToDetails(AuthClient client, string realmName) =>
        new(
            client.Id,
            client.RealmId,
            realmName,
            client.ClientId,
            client.DisplayName,
            client.Enabled,
            client.ClientSecretConfigured,
            client.ClientType,
            client.GrantTypes,
            client.RedirectUris,
            client.PostLogoutRedirectUris,
            client.Scopes,
            client.CreatedAt,
            client.UpdatedAt);
}

internal sealed record AuthAdminUpdateClientResult(
    bool Success,
    bool ClientNotFound,
    string? Error,
    AuthAdminClientDetails? Client,
    string? ClientSecret)
{
    public static AuthAdminUpdateClientResult Succeeded(
        AuthClient client,
        string realmName,
        string? clientSecret) =>
        new(
            true,
            false,
            null,
            AuthAdminCreateClientResult.ToDetails(client, realmName),
            clientSecret);

    public static AuthAdminUpdateClientResult Failed(string error) =>
        new(false, false, error, null, null);

    public static AuthAdminUpdateClientResult NotFound() =>
        new(false, true, null, null, null);
}

internal sealed record AuthAdminRotateClientSecretResult(
    bool Success,
    bool ClientNotFound,
    string? Error,
    AuthAdminClientDetails? Client,
    string? ClientSecret)
{
    public static AuthAdminRotateClientSecretResult Succeeded(
        AuthClient client,
        string realmName,
        string clientSecret) =>
        new(
            true,
            false,
            null,
            AuthAdminCreateClientResult.ToDetails(client, realmName),
            clientSecret);

    public static AuthAdminRotateClientSecretResult Failed(string error) =>
        new(false, false, error, null, null);

    public static AuthAdminRotateClientSecretResult NotFound() =>
        new(false, true, null, null, null);
}
