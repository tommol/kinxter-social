using System.Security.Claims;
using Kinxter.Accounts.Contracts;
using Kinxter.Media.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Kinxter.Media.Api;

public static class MediaEndpoints
{
    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder app, string policy)
    {
        var group = app.MapGroup("/media/avatar-uploads").WithTags("Media").RequireAuthorization(policy);
        group.MapPost("", CreateAsync).WithName("CreateAvatarUpload"); group.MapPost("/{assetId:guid}/complete", CompleteAsync).WithName("CompleteAvatarUpload"); return app;
    }
    private static async Task<IResult> CreateAsync(CreateAvatarUploadRequest request, ClaimsPrincipal principal, IAccountsService accounts, IMediaService media, CancellationToken token)
    { var account = await accounts.GetByIdentityAsync(principal.FindFirstValue("realm")!, principal.FindFirstValue("sub")!, token); if (account?.Status != Kinxter.Accounts.Model.AccountStatus.Active) return Results.Conflict(); try { return Results.Ok(await media.CreateAvatarUploadAsync(account.AccountId, request.ContentType, request.Size, token)); } catch (ArgumentException e) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = [e.Message] }); } }
    private static async Task<IResult> CompleteAsync(Guid assetId, ClaimsPrincipal principal, IAccountsService accounts, IMediaService media, CancellationToken token)
    { var account = await accounts.GetByIdentityAsync(principal.FindFirstValue("realm")!, principal.FindFirstValue("sub")!, token); return account?.Status == Kinxter.Accounts.Model.AccountStatus.Active && await media.CompleteAvatarUploadAsync(account.AccountId, assetId, token) ? Results.NoContent() : Results.NotFound(); }
    public sealed record CreateAvatarUploadRequest(string ContentType, long Size);
}
