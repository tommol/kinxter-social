using System.Security.Claims;
using Kinxter.Accounts.Contracts;
using Kinxter.Profiles.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Kinxter.Profiles.Api;

internal static class GetProfileEndpoint
{
    public static IEndpointRouteBuilder MapGetProfileEndpoint(this IEndpointRouteBuilder app, string policy)
    {
        app.MapGet("/{profileId:guid}", GetAsync).RequireAuthorization(policy).WithName("GetProfile").Produces<PublicProfileResponse>().ProducesProblem(StatusCodes.Status404NotFound); return app;
    }
    private static async Task<IResult> GetAsync(Guid profileId, ClaimsPrincipal principal, IAccountsService accounts, IProfilesService profiles, IProfileAccessEvaluator access, CancellationToken token)
    {
        var account = await accounts.GetByIdentityAsync(principal.FindFirstValue("realm")!, principal.FindFirstValue("sub")!, token); var viewer = account is null ? null : await profiles.GetByAccountIdAsync(account.AccountId, token); var target = await profiles.GetByIdAsync(profileId, token);
        if (viewer is null || target is null) return Results.NotFound(); var details = await access.CanViewDetailsAsync(viewer.ProfileId, target.ProfileId, token);
        return Results.Ok(new PublicProfileResponse(target.ProfileId, target.Handle, target.DisplayName, target.AvatarAssetId, details ? target.Bio : null, target.Visibility?.ToString(), !details));
    }
    internal sealed record PublicProfileResponse(Guid ProfileId, string Handle, string DisplayName, Guid? AvatarAssetId, string? Bio, string? Visibility, bool IsLimited);
}
