using System.Security.Claims;
using Kinxter.Accounts.Infrastructure.Persistence;
using Kinxter.IntegrationEvents.Identity;
using Kinxter.Profiles.Contracts.Dtos;
using Kinxter.Profiles.Infrastructure.Persistence;
using Kinxter.Profiles.Model;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Kinxter.Media.Contracts;
using Kinxter.Profiles.Contracts;

namespace Kinxter.Profiles.Api;

internal static class UpdateCurrentProfileEndpoint
{
    public static IEndpointRouteBuilder MapUpdateCurrentProfileEndpoints(
        this IEndpointRouteBuilder app,
        string publicUserPolicy)
    {
        app.MapPatch("/me", UpdateAsync)
            .RequireAuthorization(publicUserPolicy)
            .WithName("UpdateCurrentProfile")
            .Produces<ProfileResponseDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);
        app.MapPut("/me/visibility", SetVisibilityAsync)
            .RequireAuthorization(publicUserPolicy)
            .WithName("SetCurrentProfileVisibility")
            .Produces<ProfileResponseDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> UpdateAsync(
        UpdateCurrentProfileRequestDto request,
        ClaimsPrincipal principal,
        AccountsDbContext accountsDbContext,
        ProfilesDbContext profilesDbContext,
        IMediaService mediaService,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var errors = ProfileEndpointValidation.ValidateRequiredText(
        [
            (nameof(request.Handle), request.Handle, Profile.HandleMaxLength),
            (nameof(request.DisplayName), request.DisplayName, Profile.DisplayNameMaxLength)
        ]);

        try
        {
            _ = Profile.NormalizeHandle(request.Handle);
        }
        catch (ArgumentException exception)
        {
            errors[nameof(request.Handle)] = [exception.Message];
        }

        if (!string.IsNullOrWhiteSpace(request.Bio) && request.Bio.Trim().Length > Profile.BioMaxLength)
        {
            errors[nameof(request.Bio)] = [$"Bio cannot be longer than {Profile.BioMaxLength} characters."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var accountId = await FindAccountIdAsync(principal, accountsDbContext, cancellationToken);
        var profile = await profilesDbContext.Profiles
            .SingleOrDefaultAsync(current => current.AccountId == accountId, cancellationToken);

        if (profile is null)
        {
            return Results.NotFound();
        }

        if (request.AvatarAssetId is Guid avatarAssetId &&
            !await mediaService.IsReadyAndOwnedAsync(accountId, avatarAssetId, cancellationToken))
        {
            return Results.Conflict(new { error = "Avatar asset is not ready or does not belong to the current account." });
        }

        profile.UpdateDetails(request.Handle, request.DisplayName, request.Bio, request.AvatarAssetId, clock.UtcNow);
        try
        {
            await profilesDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { error = "Handle is already taken." });
        }
        return Results.Ok(ProfileResponseDto.From(profile));
    }

    private static async Task<IResult> SetVisibilityAsync(
        SetProfileVisibilityRequestDto request,
        ClaimsPrincipal principal,
        AccountsDbContext accountsDbContext,
        ProfilesDbContext profilesDbContext,
        IEnumerable<IProfileVisibilityChangedListener> listeners,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ProfileVisibility>(request.Visibility, true, out var visibility))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Visibility)] = ["Visibility must be Public or Private."]
            });
        }

        var accountId = await FindAccountIdAsync(principal, accountsDbContext, cancellationToken);
        var profile = await profilesDbContext.Profiles
            .SingleOrDefaultAsync(current => current.AccountId == accountId, cancellationToken);

        if (profile is null)
        {
            return Results.NotFound();
        }

        profile.SetVisibility(visibility, clock.UtcNow);
        await profilesDbContext.SaveChangesAsync(cancellationToken);
        foreach (var listener in listeners)
        {
            await listener.OnChangedAsync(profile.Id, visibility, cancellationToken);
        }
        return Results.Ok(ProfileResponseDto.From(profile));
    }

    private static Task<Guid> FindAccountIdAsync(
        ClaimsPrincipal principal,
        AccountsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var subject = principal.FindFirstValue("sub")!;
        var realm = principal.FindFirstValue("realm")!;
        var provider = KinxterAuthIdentityProvider.ForRealm(realm);
        return dbContext.Accounts
            .Where(account => account.IdentityProvider == provider && account.IdentitySubject == subject)
            .Select(account => account.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
