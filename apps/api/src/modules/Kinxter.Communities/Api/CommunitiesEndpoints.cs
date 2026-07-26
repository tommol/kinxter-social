using System.Security.Claims;
using Kinxter.Accounts.Contracts;
using Kinxter.Communities.Contracts;
using Kinxter.Communities.Infrastructure;
using Kinxter.Communities.Model;
using Kinxter.Locations.Contracts;
using Kinxter.Profiles.Contracts;
using Kinxter.Shared.Abstractions.Time;
using Kinxter.Tags.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Communities.Api;

public static class CommunitiesEndpoints
{
    public static IEndpointRouteBuilder MapCommunitiesEndpoints(this IEndpointRouteBuilder app, string publicPolicy, string moderationPolicy)
    {
        var group = app.MapGroup("/communities").WithTags("Communities").RequireAuthorization(publicPolicy);
        group.MapPost("", CreateAsync).WithName("CreateCommunity");
        group.MapPost("/{communityId:guid}/submit", SubmitAsync).WithName("SubmitCommunityForReview");
        group.MapPost("/{communityId:guid}/memberships/me", JoinAsync).WithName("JoinCommunity");
        var admin = app.MapGroup("/admin/communities").WithTags("Community moderation").RequireAuthorization(moderationPolicy);
        admin.MapGet("/pending", async (CommunitiesDbContext db, CancellationToken token) => Results.Ok(await db.Communities.AsNoTracking().Where(c => c.Status == CommunityStatus.PendingReview).OrderBy(c => c.CreatedAt).ToArrayAsync(token)));
        admin.MapPost("/{communityId:guid}/publish", PublishAsync);
        admin.MapPost("/{communityId:guid}/reject", RejectAsync);
        return app;
    }

    private static async Task<IResult> CreateAsync(CreateCommunityRequest request, ClaimsPrincipal principal, IAccountsService accounts, IProfilesService profiles, CommunitiesDbContext db, ITagsService tags, ILocationsService locations, IClock clock, CancellationToken token)
    {
        var account = await CurrentAccountAsync(principal, accounts, token); var profile = account is null ? null : await profiles.GetByAccountIdAsync(account.AccountId, token);
        if (profile?.OnboardingCompletedAt is null) return Results.Conflict(new { error = "Onboarding must be completed before creating a community." });
        try
        {
            var community = new Community(Guid.CreateVersion7(clock.UtcNow), profile.ProfileId, request.Slug, request.Name, request.Description, clock.UtcNow);
            db.Communities.Add(community); db.Memberships.Add(new CommunityMembership(community.Id, profile.ProfileId, true, clock.UtcNow)); await db.SaveChangesAsync(token);
            await tags.SetTagsAsync("community", community.Id, request.TagIds, token);
            if (request.PlaceId is not null) await locations.SetForEntityAsync("community", community.Id, request.PlaceId.Value, token);
            return Results.Created($"/api/v1/communities/{community.Id}", community);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or DbUpdateException) { return Results.Conflict(new { error = exception.Message }); }
    }
    private static async Task<IResult> SubmitAsync(Guid communityId, ClaimsPrincipal principal, IAccountsService accounts, IProfilesService profiles, CommunitiesDbContext db, IClock clock, CancellationToken token)
    {
        var account = await CurrentAccountAsync(principal, accounts, token); var profile = account is null ? null : await profiles.GetByAccountIdAsync(account.AccountId, token);
        var community = profile is null ? null : await db.Communities.SingleOrDefaultAsync(c => c.Id == communityId && c.OwnerProfileId == profile.ProfileId, token);
        if (community is null) return Results.NotFound();
        try { community.Submit(clock.UtcNow); await db.SaveChangesAsync(token); return Results.Ok(community); } catch (InvalidOperationException e) { return Results.Conflict(new { error = e.Message }); }
    }
    private static async Task<IResult> JoinAsync(Guid communityId, ClaimsPrincipal principal, IAccountsService accounts, IProfilesService profiles, ICommunitiesService communities, CancellationToken token)
    {
        var account = await CurrentAccountAsync(principal, accounts, token); var profile = account is null ? null : await profiles.GetByAccountIdAsync(account.AccountId, token);
        if (profile is null) return Results.Conflict(); return await communities.JoinAsync(communityId, profile.ProfileId, token) ? Results.NoContent() : Results.NotFound();
    }
    private static async Task<IResult> PublishAsync(Guid communityId, CommunitiesDbContext db, IClock clock, CancellationToken token) { var c = await db.Communities.SingleOrDefaultAsync(x => x.Id == communityId, token); if (c is null) return Results.NotFound(); try { c.Publish(clock.UtcNow); await db.SaveChangesAsync(token); return Results.Ok(c); } catch (InvalidOperationException e) { return Results.Conflict(new { error = e.Message }); } }
    private static async Task<IResult> RejectAsync(Guid communityId, RejectCommunityRequest request, CommunitiesDbContext db, IClock clock, CancellationToken token) { var c = await db.Communities.SingleOrDefaultAsync(x => x.Id == communityId, token); if (c is null) return Results.NotFound(); try { c.Reject(request.Reason, clock.UtcNow); await db.SaveChangesAsync(token); return Results.Ok(c); } catch (Exception e) when (e is InvalidOperationException or ArgumentException) { return Results.Conflict(new { error = e.Message }); } }
    private static Task<AccountState?> CurrentAccountAsync(ClaimsPrincipal principal, IAccountsService accounts, CancellationToken token) => accounts.GetByIdentityAsync(principal.FindFirstValue("realm")!, principal.FindFirstValue("sub")!, token);
    public sealed record CreateCommunityRequest(string Slug, string Name, string Description, IReadOnlyCollection<Guid> TagIds, long? PlaceId);
    public sealed record RejectCommunityRequest(string Reason);
}
