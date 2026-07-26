using System.Security.Claims;
using Kinxter.Accounts.Contracts;
using Kinxter.Locations.Contracts;
using Kinxter.Profiles.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Locations.Api;

public static class LocationsEndpoints
{
    public static IEndpointRouteBuilder MapLocationsEndpoints(this IEndpointRouteBuilder app, string publicPolicy)
    {
        var group = app.MapGroup("/locations").WithTags("Locations").RequireAuthorization(publicPolicy);
        group.MapGet("/places", async (string query, int? limit, ILocationsService service, CancellationToken token) => Results.Ok((await service.SearchAsync(query, limit ?? 10, token)).Select(place => new { place.PlaceId, place.DisplayName }))).WithName("SearchPlaces");
        group.MapPut("/me", SetCurrentAsync).WithName("SetCurrentProfileLocation");
        group.MapDelete("/me", DeleteCurrentAsync).WithName("ClearCurrentProfileLocation");
        group.MapGet("/profiles/{profileId:guid}", GetProfileLocationAsync).WithName("GetProfileLocation");
        return app;
    }

    private static async Task<IResult> GetProfileLocationAsync(Guid profileId, ClaimsPrincipal principal, IAccountsService accounts, IProfilesService profiles, IProfileAccessEvaluator access, ILocationsService locations, CancellationToken token)
    {
        var account = await accounts.GetByIdentityAsync(principal.FindFirstValue("realm")!, principal.FindFirstValue("sub")!, token); var viewer = account is null ? null : await profiles.GetByAccountIdAsync(account.AccountId, token);
        if (viewer is null || !await access.CanViewDetailsAsync(viewer.ProfileId, profileId, token)) return Results.Forbid();
        var place = await locations.GetForEntityAsync("profile", profileId, token); return place is null ? Results.NotFound() : Results.Ok(new { place.PlaceId, place.DisplayName });
    }

    private static async Task<IResult> SetCurrentAsync(SetLocationRequest request, ClaimsPrincipal principal, IAccountsService accounts, IProfilesService profiles, ILocationsService locations, CancellationToken token)
    {
        var account = await accounts.GetByIdentityAsync(principal.FindFirstValue("realm")!, principal.FindFirstValue("sub")!, token);
        var profile = account is null ? null : await profiles.GetByAccountIdAsync(account.AccountId, token);
        if (profile is null) return Results.Conflict(new { error = "Profile must be created first." });
        try { await locations.SetForEntityAsync("profile", profile.ProfileId, request.PlaceId, token); return Results.NoContent(); }
        catch (ArgumentException exception) { return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.PlaceId)] = [exception.Message] }); }
    }

    private static async Task<IResult> DeleteCurrentAsync(ClaimsPrincipal principal, IAccountsService accounts, IProfilesService profiles, Infrastructure.LocationsDbContext db, CancellationToken token)
    {
        var account = await accounts.GetByIdentityAsync(principal.FindFirstValue("realm")!, principal.FindFirstValue("sub")!, token);
        var profile = account is null ? null : await profiles.GetByAccountIdAsync(account.AccountId, token);
        if (profile is null) return Results.NoContent();
        await db.EntityLocations.Where(current => current.EntityType == "profile" && current.EntityId == profile.ProfileId).ExecuteDeleteAsync(token);
        return Results.NoContent();
    }

    public sealed record SetLocationRequest(long PlaceId);
}
