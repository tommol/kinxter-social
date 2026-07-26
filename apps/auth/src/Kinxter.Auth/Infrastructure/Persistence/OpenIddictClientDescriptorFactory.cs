using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Kinxter.Auth.Infrastructure.Persistence;

internal static class OpenIddictClientDescriptorFactory
{
    public static OpenIddictApplicationDescriptor Create(
        AuthClient client,
        string? clientSecret = null)
    {
        ArgumentNullException.ThrowIfNull(client);

        return Create(
            client.ClientId,
            client.DisplayName,
            client.ClientType,
            client.GrantTypes,
            client.RedirectUris,
            client.PostLogoutRedirectUris,
            client.Scopes,
            clientSecret);
    }

    public static OpenIddictApplicationDescriptor Create(
        AuthClientOptions client)
    {
        ArgumentNullException.ThrowIfNull(client);

        return Create(
            client.ClientId,
            client.DisplayName,
            client.ClientType,
            client.GrantTypes,
            client.RedirectUris,
            client.PostLogoutRedirectUris,
            client.Scopes,
            client.ClientType == AuthClientType.Confidential
                ? client.ClientSecret
                : null);
    }

    public static void ApplyRegistration(
        OpenIddictApplicationDescriptor descriptor,
        AuthClient client)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(client);

        descriptor.ClientId = client.ClientId;
        descriptor.ClientType = client.ClientType switch
        {
            AuthClientType.Public => ClientTypes.Public,
            AuthClientType.Confidential => ClientTypes.Confidential,
            _ => throw new InvalidOperationException($"Unsupported client type '{client.ClientType}'.")
        };
        descriptor.ConsentType = ConsentTypes.Implicit;
        descriptor.DisplayName = client.DisplayName;

        descriptor.Permissions.Clear();
        descriptor.Permissions.UnionWith(CreatePermissions(client.GrantTypes, client.Scopes));

        descriptor.Requirements.Clear();
        if (client.GrantTypes.Contains(AuthClientGrantTypes.AuthorizationCode, StringComparer.Ordinal))
        {
            descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
        }

        descriptor.RedirectUris.Clear();
        foreach (var redirectUri in client.RedirectUris)
        {
            descriptor.RedirectUris.Add(new Uri(redirectUri));
        }

        descriptor.PostLogoutRedirectUris.Clear();
        foreach (var logoutUri in client.PostLogoutRedirectUris)
        {
            descriptor.PostLogoutRedirectUris.Add(new Uri(logoutUri));
        }
    }

    private static OpenIddictApplicationDescriptor Create(
        string clientId,
        string displayName,
        AuthClientType clientType,
        IEnumerable<string> grantTypes,
        IEnumerable<string> redirectUris,
        IEnumerable<string> postLogoutRedirectUris,
        IEnumerable<string> scopes,
        string? clientSecret)
    {
        var client = new AuthClient
        {
            ClientId = clientId,
            DisplayName = displayName,
            ClientType = clientType,
            GrantTypes = CleanValues(grantTypes),
            RedirectUris = CleanValues(redirectUris),
            PostLogoutRedirectUris = CleanValues(postLogoutRedirectUris),
            Scopes = CleanValues(scopes)
        };
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientSecret = clientSecret
        };

        ApplyRegistration(descriptor, client);

        return descriptor;
    }

    private static IEnumerable<string> CreatePermissions(
        IEnumerable<string> grantTypes,
        IEnumerable<string> scopes)
    {
        var grants = grantTypes.ToHashSet(StringComparer.Ordinal);

        if (grants.Contains(AuthClientGrantTypes.AuthorizationCode))
        {
            yield return Permissions.Endpoints.Authorization;
            yield return Permissions.Endpoints.EndSession;
            yield return Permissions.Endpoints.PushedAuthorization;
            yield return Permissions.Endpoints.Token;
            yield return Permissions.GrantTypes.AuthorizationCode;
            yield return Permissions.ResponseTypes.Code;
        }

        if (grants.Contains(AuthClientGrantTypes.ClientCredentials))
        {
            yield return Permissions.Endpoints.Token;
            yield return Permissions.GrantTypes.ClientCredentials;
        }

        if (grants.Contains(AuthClientGrantTypes.DeviceCode))
        {
            yield return Permissions.Endpoints.DeviceAuthorization;
            yield return Permissions.Endpoints.Token;
            yield return Permissions.Prefixes.GrantType + AuthClientGrantTypes.DeviceCode;
        }

        if (grants.Contains(AuthClientGrantTypes.RefreshToken))
        {
            yield return Permissions.Endpoints.Token;
            yield return Permissions.GrantTypes.RefreshToken;
        }

        if (grants.Count > 0)
        {
            yield return Permissions.Endpoints.Revocation;
        }

        foreach (var scope in scopes.Distinct(StringComparer.Ordinal))
        {
            var permission = scope switch
            {
                Scopes.Email => Permissions.Scopes.Email,
                Scopes.Profile => Permissions.Scopes.Profile,
                Scopes.Roles => Permissions.Scopes.Roles,
                Scopes.OpenId or Scopes.OfflineAccess => null,
                _ => Permissions.Prefixes.Scope + scope
            };

            if (permission is not null)
            {
                yield return permission;
            }
        }
    }

    private static string[] CleanValues(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
