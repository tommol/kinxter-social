using Kinxter.Auth.Infrastructure.Persistence;

namespace Kinxter.Auth;

internal sealed class AuthRealmRegistry
{
    private readonly AuthServerOptions bootstrapOptions;
    private AuthOptions[] realms;

    public AuthRealmRegistry(AuthServerOptions bootstrapOptions)
    {
        ArgumentNullException.ThrowIfNull(bootstrapOptions);

        this.bootstrapOptions = bootstrapOptions;
        this.realms = bootstrapOptions.Realms;
    }

    public IReadOnlyList<AuthOptions> Realms => Volatile.Read(ref this.realms);

    public string[] AllowedOrigins =>
        Realms
            .SelectMany(realm => realm.AllowedOrigins)
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public bool TryFindByPath(PathString path, out AuthOptions options, out PathString remaining)
    {
        foreach (var realm in Realms.OrderByDescending(realm => realm.PathBase.Length))
        {
            if (path.StartsWithSegments(new PathString(realm.PathBase), out remaining))
            {
                options = realm;

                return true;
            }
        }

        options = null!;
        remaining = PathString.Empty;

        return false;
    }

    public bool TryFindByRealm(string realm, out AuthOptions options)
    {
        options = Realms.SingleOrDefault(current =>
            string.Equals(current.Realm, realm, StringComparison.OrdinalIgnoreCase))!;

        return options is not null;
    }

    public void Replace(IEnumerable<AuthRealm> persistedRealms)
    {
        ArgumentNullException.ThrowIfNull(persistedRealms);

        var updatedRealms = persistedRealms
            .OrderBy(realm => realm.Name, StringComparer.Ordinal)
            .Select(Map)
            .ToArray();

        Volatile.Write(ref this.realms, updatedRealms);
    }

    public void Update(AuthRealm persistedRealm)
    {
        ArgumentNullException.ThrowIfNull(persistedRealm);

        var updatedRealm = Map(persistedRealm);
        var updatedRealms = Realms
            .Where(realm => !string.Equals(
                realm.Realm,
                persistedRealm.Name,
                StringComparison.OrdinalIgnoreCase))
            .Append(updatedRealm)
            .OrderBy(realm => realm.Realm, StringComparer.Ordinal)
            .ToArray();

        Volatile.Write(ref this.realms, updatedRealms);
    }

    private AuthOptions Map(AuthRealm persistedRealm)
    {
        this.bootstrapOptions.TryFindByRealm(persistedRealm.Name, out var bootstrapRealm);

        return new AuthOptions
        {
            Realm = persistedRealm.Name,
            Issuer = persistedRealm.Issuer,
            PathBase = persistedRealm.PathBase,
            DbSchema = bootstrapRealm?.DbSchema ?? this.bootstrapOptions.DbSchema,
            CookieName = bootstrapRealm?.CookieName ?? this.bootstrapOptions.CookieName,
            MfaPolicy = persistedRealm.MfaPolicy,
            SignupEnabled = persistedRealm.SignupEnabled,
            EncryptionKey = bootstrapRealm?.EncryptionKey ?? this.bootstrapOptions.EncryptionKey,
            AllowedOrigins = bootstrapRealm?.AllowedOrigins ?? [],
            Clients = bootstrapRealm?.Clients ?? [],
            ExternalProviders = bootstrapRealm?.ExternalProviders ?? new AuthExternalProvidersOptions(),
            SeedAdmin = bootstrapRealm?.SeedAdmin ?? new SeedAdminOptions()
        };
    }
}
