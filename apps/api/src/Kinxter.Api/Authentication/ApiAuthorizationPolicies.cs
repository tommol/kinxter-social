namespace Kinxter.Api.Authentication;

internal static class ApiAuthorizationPolicies
{
    public const string PublicUser = "PublicUser";
    public const string BackofficeAdmin = "BackofficeAdmin";
    public const string MonitoringRead = "MonitoringRead";
    public const string ModerationRead = "ModerationRead";
    public const string ModerationWrite = "ModerationWrite";
    public const string UsersRead = "UsersRead";
    public const string UsersManage = "UsersManage";
    public const string AdminUsersManage = "AdminUsersManage";
}

internal static class ApiPermissions
{
    public const string AdminAccess = "admin.access";
    public const string MonitoringRead = "monitoring.read";
    public const string ModerationRead = "moderation.read";
    public const string ModerationWrite = "moderation.write";
    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";
    public const string AdminUsersManage = "admin_users.manage";
}
