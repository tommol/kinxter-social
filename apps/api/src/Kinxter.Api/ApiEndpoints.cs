using Kinxter.Accounts.Api;
using Kinxter.Api.Authentication;
using Kinxter.Profiles.Api;
using Kinxter.Tags.Api;
using Kinxter.Locations.Api;
using Kinxter.Communities.Api;
using Kinxter.SocialGraph.Api;
using Kinxter.Recommendations.Api;
using Kinxter.Onboarding.Api;
using Kinxter.Media.Api;

namespace Kinxter.Api;

internal static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApiV1(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1");

        group.MapAccountsEndpoints(ApiAuthorizationPolicies.PublicUser);
        group.MapCurrentUserEndpoints();
        group.MapProfilesEndpoints(ApiAuthorizationPolicies.PublicUser);
        group.MapTagsEndpoints(ApiAuthorizationPolicies.PublicUser, ApiAuthorizationPolicies.TaxonomyManage);
        group.MapLocationsEndpoints(ApiAuthorizationPolicies.PublicUser);
        group.MapCommunitiesEndpoints(ApiAuthorizationPolicies.PublicUser, ApiAuthorizationPolicies.CommunitiesModerate);
        group.MapSocialGraphEndpoints(ApiAuthorizationPolicies.PublicUser);
        group.MapRecommendationsEndpoints(ApiAuthorizationPolicies.PublicUser);
        group.MapOnboardingEndpoints(ApiAuthorizationPolicies.PublicUser);
        group.MapMediaEndpoints(ApiAuthorizationPolicies.PublicUser);
        group.MapMonitoringEndpoints();

        return app;
    }
}
