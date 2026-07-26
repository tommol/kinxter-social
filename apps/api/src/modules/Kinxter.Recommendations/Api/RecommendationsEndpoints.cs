using System.Security.Claims;
using Kinxter.Accounts.Contracts;
using Kinxter.Profiles.Contracts;
using Kinxter.Recommendations.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Kinxter.Recommendations.Api;

public static class RecommendationsEndpoints
{
    public static IEndpointRouteBuilder MapRecommendationsEndpoints(this IEndpointRouteBuilder app, string policy)
    {
        app.MapGet("/recommendations/onboarding", GetAsync).RequireAuthorization(policy).WithTags("Recommendations").WithName("GetOnboardingRecommendations").Produces<OnboardingRecommendations>();
        return app;
    }
    private static async Task<IResult> GetAsync(int? limit, ClaimsPrincipal principal, IAccountsService accounts, IProfilesService profiles, IRecommendationsService recommendations, CancellationToken token)
    {
        var account = await accounts.GetByIdentityAsync(principal.FindFirstValue("realm")!, principal.FindFirstValue("sub")!, token);
        var profile = account is null ? null : await profiles.GetByAccountIdAsync(account.AccountId, token);
        return profile is null ? Results.Conflict(new { error = "Profile must be created first." }) : Results.Ok(await recommendations.GetOnboardingAsync(profile.ProfileId, limit ?? 20, token));
    }
}
