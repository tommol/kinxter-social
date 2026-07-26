using System.Security.Claims;
using Kinxter.Accounts.Contracts;
using Kinxter.Profiles.Contracts;
using Kinxter.SocialGraph.Contracts;
using Kinxter.SocialGraph.Infrastructure;
using Kinxter.SocialGraph.Model;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.SocialGraph.Api;

public static class SocialGraphEndpoints
{
    public static IEndpointRouteBuilder MapSocialGraphEndpoints(this IEndpointRouteBuilder app, string policy)
    {
        var group = app.MapGroup("/social-graph").WithTags("Social graph").RequireAuthorization(policy);
        group.MapPut("/follows/{profileId:guid}", FollowAsync).WithName("FollowProfile");
        group.MapDelete("/follows/{profileId:guid}", UnfollowAsync).WithName("UnfollowProfile");
        group.MapGet("/follow-requests", RequestsAsync).WithName("ListFollowRequests");
        group.MapPost("/follow-requests/{requesterId:guid}/accept", AcceptAsync).WithName("AcceptFollowRequest");
        group.MapPost("/follow-requests/{requesterId:guid}/reject", RejectAsync).WithName("RejectFollowRequest");
        return app;
    }
    private static async Task<IResult> FollowAsync(Guid profileId, ClaimsPrincipal principal, IAccountsService accounts, IProfilesService profiles, ISocialGraphService graph, CancellationToken token)
    { var me = await CurrentProfileAsync(principal, accounts, profiles, token); if (me is null) return Results.Conflict(); try { return Results.Ok(new { status = (await graph.FollowAsync(me.ProfileId, profileId, token)).ToString() }); } catch (KeyNotFoundException) { return Results.NotFound(); } catch (ArgumentException e) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["profileId"] = [e.Message] }); } }
    private static async Task<IResult> UnfollowAsync(Guid profileId, ClaimsPrincipal principal, IAccountsService accounts, IProfilesService profiles, ISocialGraphService graph, CancellationToken token) { var me = await CurrentProfileAsync(principal, accounts, profiles, token); if (me is not null) await graph.UnfollowAsync(me.ProfileId, profileId, token); return Results.NoContent(); }
    private static async Task<IResult> RequestsAsync(ClaimsPrincipal principal, IAccountsService accounts, IProfilesService profiles, SocialGraphDbContext db, CancellationToken token) { var me = await CurrentProfileAsync(principal, accounts, profiles, token); if (me is null) return Results.Ok(Array.Empty<object>()); return Results.Ok(await db.Follows.AsNoTracking().Where(f => f.FollowedProfileId == me.ProfileId && f.Status == FollowStatus.Pending).Select(f => new { requesterProfileId = f.FollowerProfileId, f.CreatedAt }).ToArrayAsync(token)); }
    private static async Task<IResult> AcceptAsync(Guid requesterId, ClaimsPrincipal principal, IAccountsService accounts, IProfilesService profiles, SocialGraphDbContext db, IClock clock, CancellationToken token) => await DecideAsync(requesterId, true, principal, accounts, profiles, db, clock, token);
    private static async Task<IResult> RejectAsync(Guid requesterId, ClaimsPrincipal principal, IAccountsService accounts, IProfilesService profiles, SocialGraphDbContext db, IClock clock, CancellationToken token) => await DecideAsync(requesterId, false, principal, accounts, profiles, db, clock, token);
    private static async Task<IResult> DecideAsync(Guid requesterId, bool accept, ClaimsPrincipal principal, IAccountsService accounts, IProfilesService profiles, SocialGraphDbContext db, IClock clock, CancellationToken token) { var me = await CurrentProfileAsync(principal, accounts, profiles, token); var follow = me is null ? null : await db.Follows.SingleOrDefaultAsync(f => f.FollowerProfileId == requesterId && f.FollowedProfileId == me.ProfileId && f.Status == FollowStatus.Pending, token); if (follow is null) return Results.NotFound(); if (accept) follow.Accept(clock.UtcNow); else follow.Reject(clock.UtcNow); await db.SaveChangesAsync(token); return Results.NoContent(); }
    private static async Task<ProfileState?> CurrentProfileAsync(ClaimsPrincipal principal, IAccountsService accounts, IProfilesService profiles, CancellationToken token) { var account = await accounts.GetByIdentityAsync(principal.FindFirstValue("realm")!, principal.FindFirstValue("sub")!, token); return account is null ? null : await profiles.GetByAccountIdAsync(account.AccountId, token); }
}
