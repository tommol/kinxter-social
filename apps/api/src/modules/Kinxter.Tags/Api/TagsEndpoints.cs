using System.Security.Claims;
using Kinxter.Accounts.Contracts;
using Kinxter.Profiles.Contracts;
using Kinxter.Shared.Abstractions.Time;
using Kinxter.Tags.Contracts;
using Kinxter.Tags.Infrastructure;
using Kinxter.Tags.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Tags.Api;

public static class TagsEndpoints
{
    public static IEndpointRouteBuilder MapTagsEndpoints(this IEndpointRouteBuilder app, string publicPolicy, string adminPolicy)
    {
        var group = app.MapGroup("/tags").WithTags("Kinktags");
        group.MapGet("", async (ITagsService service, CancellationToken token) => Results.Ok(await service.GetActiveAsync(token)))
            .RequireAuthorization(publicPolicy).WithName("ListKinktags");
        group.MapPut("/me", SetCurrentAsync).RequireAuthorization(publicPolicy).WithName("SetCurrentProfileKinktags");
        group.MapGet("/profiles/{profileId:guid}", GetProfileTagsAsync).RequireAuthorization(publicPolicy).WithName("GetProfileKinktags");

        var admin = app.MapGroup("/admin/tags").WithTags("Admin kinktags").RequireAuthorization(adminPolicy);
        admin.MapGet("", async (TagsDbContext db, CancellationToken token) => Results.Ok(await db.Tags.AsNoTracking().OrderBy(tag => tag.SortOrder).ToArrayAsync(token)));
        admin.MapPost("", CreateAsync);
        admin.MapPut("/{tagId:guid}", UpdateAsync);
        return app;
    }

    private static async Task<IResult> GetProfileTagsAsync(Guid profileId, ClaimsPrincipal principal, IAccountsService accounts, IProfilesService profiles, IProfileAccessEvaluator access, ITagsService tags, CancellationToken token)
    {
        var account = await accounts.GetByIdentityAsync(principal.FindFirstValue("realm")!, principal.FindFirstValue("sub")!, token); var viewer = account is null ? null : await profiles.GetByAccountIdAsync(account.AccountId, token);
        if (viewer is null || !await access.CanViewDetailsAsync(viewer.ProfileId, profileId, token)) return Results.Forbid();
        var ids = await tags.GetTagIdsAsync("profile", profileId, token); var catalog = await tags.GetActiveAsync(token); return Results.Ok(catalog.Where(tag => ids.Contains(tag.Id)));
    }

    private static async Task<IResult> SetCurrentAsync(
        SetTagsRequest request,
        ClaimsPrincipal principal,
        IAccountsService accounts,
        IProfilesService profiles,
        ITagsService tags,
        CancellationToken cancellationToken)
    {
        var account = await accounts.GetByIdentityAsync(principal.FindFirstValue("realm")!, principal.FindFirstValue("sub")!, cancellationToken);
        var profile = account is null ? null : await profiles.GetByAccountIdAsync(account.AccountId, cancellationToken);

        if (profile is null) return Results.Conflict(new { error = "Profile must be created first." });
        if (request.TagIds.Count > 20) return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.TagIds)] = ["At most 20 kinktags can be selected."] });

        try
        {
            await tags.SetTagsAsync("profile", profile.ProfileId, request.TagIds, cancellationToken);
            return Results.NoContent();
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.TagIds)] = [exception.Message] });
        }
    }

    private static async Task<IResult> CreateAsync(CreateTagRequest request, TagsDbContext db, IClock clock, CancellationToken token)
    {
        try
        {
            var tag = new KinkTag(Guid.CreateVersion7(clock.UtcNow), request.Slug, request.Category, request.NamePl, request.NameEn, request.DescriptionPl, request.DescriptionEn, request.SortOrder, clock.UtcNow);
            db.Tags.Add(tag);
            await db.SaveChangesAsync(token);
            return Results.Created($"/api/v1/admin/tags/{tag.Id}", tag);
        }
        catch (Exception exception) when (exception is ArgumentException or DbUpdateException)
        {
            return Results.Conflict(new { error = exception.Message });
        }
    }

    private static async Task<IResult> UpdateAsync(Guid tagId, UpdateTagRequest request, TagsDbContext db, IClock clock, CancellationToken token)
    {
        var tag = await db.Tags.SingleOrDefaultAsync(current => current.Id == tagId, token);
        if (tag is null) return Results.NotFound();
        tag.Update(request.Category, request.NamePl, request.NameEn, request.DescriptionPl, request.DescriptionEn, request.SortOrder, request.IsActive, clock.UtcNow);
        await db.SaveChangesAsync(token);
        return Results.Ok(tag);
    }

    public sealed record SetTagsRequest(IReadOnlyCollection<Guid> TagIds);
    public sealed record CreateTagRequest(string Slug, string Category, string NamePl, string NameEn, string? DescriptionPl, string? DescriptionEn, int SortOrder);
    public sealed record UpdateTagRequest(string Category, string NamePl, string NameEn, string? DescriptionPl, string? DescriptionEn, int SortOrder, bool IsActive);
}
