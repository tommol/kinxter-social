using Kinxter.Accounts.Api;
using Kinxter.Api.Authentication;
using Kinxter.Profiles.Api;

namespace Kinxter.Api;

internal static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApiV1(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1");

        group.MapAccountsEndpoints();
        group.MapCurrentUserEndpoints();
        group.MapProfilesEndpoints(ApiAuthorizationPolicies.PublicUser);
        group.MapMonitoringEndpoints();

        return app;
    }
}
