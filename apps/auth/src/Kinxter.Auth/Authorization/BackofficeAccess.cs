namespace Kinxter.Auth;

internal static class AuthRealmNames
{
    public const string Backoffice = "backoffice";
}

internal static class AuthClaimTypes
{
    public const string Permission = "permission";
}

internal static class AuthPermissions
{
    public const string AdminAccess = "admin.access";
    public const string MonitoringRead = "monitoring.read";
    public const string ModerationRead = "moderation.read";
    public const string ModerationWrite = "moderation.write";
    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";
    public const string AdminUsersManage = "admin_users.manage";
    public const string TaxonomyManage = "taxonomy.manage";
    public const string CommunitiesModerate = "communities.moderate";

    public static readonly IReadOnlyList<string> All =
    [
        AdminAccess,
        MonitoringRead,
        ModerationRead,
        ModerationWrite,
        UsersRead,
        UsersManage,
        AdminUsersManage,
        TaxonomyManage,
        CommunitiesModerate
    ];
}

internal static class AuthRoles
{
    public const string SuperAdmin = "super_admin";
    public const string Operations = "ops";
    public const string Moderator = "moderator";
    public const string Support = "support";
    public const string ReadOnly = "read_only";

    // Kept so existing deployments using the original role continue to work.
    public const string LegacyAdmin = "admin";

    public static readonly IReadOnlyList<AuthRoleDefinition> Assignable =
    [
        new(
            SuperAdmin,
            "Super administrator",
            "Full backoffice access, including administrator management.",
            AuthPermissions.All),
        new(
            Operations,
            "Operations",
            "Operational monitoring and diagnostics.",
            [AuthPermissions.AdminAccess, AuthPermissions.MonitoringRead]),
        new(
            Moderator,
            "Moderator",
            "Content and community moderation.",
            [
                AuthPermissions.AdminAccess,
                AuthPermissions.ModerationRead,
                AuthPermissions.ModerationWrite,
                AuthPermissions.TaxonomyManage,
                AuthPermissions.CommunitiesModerate,
                AuthPermissions.UsersRead
            ]),
        new(
            Support,
            "Support",
            "User support and account management.",
            [
                AuthPermissions.AdminAccess,
                AuthPermissions.UsersRead,
                AuthPermissions.UsersManage
            ]),
        new(
            ReadOnly,
            "Read only",
            "Read-only access to monitoring, moderation and users.",
            [
                AuthPermissions.AdminAccess,
                AuthPermissions.MonitoringRead,
                AuthPermissions.ModerationRead,
                AuthPermissions.UsersRead
            ])
    ];

    public static readonly IReadOnlyList<string> AllNames =
        Assignable.Select(role => role.Name).Append(LegacyAdmin).ToArray();

    public static bool IsAssignable(string role) =>
        Assignable.Any(definition => string.Equals(definition.Name, role, StringComparison.Ordinal));

    public static string[] GetPermissions(IEnumerable<string> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);

        var requestedRoles = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (requestedRoles.Contains(LegacyAdmin))
        {
            return AuthPermissions.All.ToArray();
        }

        return Assignable
            .Where(role => requestedRoles.Contains(role.Name))
            .SelectMany(role => role.Permissions)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(permission => permission, StringComparer.Ordinal)
            .ToArray();
    }
}

internal sealed record AuthRoleDefinition(
    string Name,
    string DisplayName,
    string Description,
    IReadOnlyList<string> Permissions);
