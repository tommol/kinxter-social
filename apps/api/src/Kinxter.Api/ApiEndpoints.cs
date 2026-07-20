using Kinxter.Accounts.Api;

namespace Kinxter.Api;

internal static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApiV1(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1");

        group.MapAccountsEndpoints();
        group.MapOnboardingEndpoints();
        group.MapMonitoringEndpoints();

        return app;
    }
}
