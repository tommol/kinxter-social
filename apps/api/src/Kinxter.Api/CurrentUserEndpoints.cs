using System.Security.Claims;
using Kinxter.Accounts.Infrastructure.Persistence;
using Kinxter.Api.Authentication;
using Kinxter.Api.Contracts.Dtos;
using Kinxter.Profiles.Infrastructure.Persistence;
using Kinxter.Profiles.Model;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Api;

internal static class CurrentUserEndpoints
{
    public static IEndpointRouteBuilder MapCurrentUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("")
            .WithTags("Current user")
            .RequireAuthorization(ApiAuthorizationPolicies.PublicUser);

        group.MapGet("/me", GetCurrentUserAsync)
            .WithName("GetCurrentUser")
            .WithSummary("Returns the current public user's account and profile state.")
            .Produces<CurrentUserResponseDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        ApiAuthenticationOptions authOptions,
        AccountsDbContext accountsDbContext,
        ProfilesDbContext profilesDbContext,
        CancellationToken cancellationToken)
    {
        var identity = GetIdentity(principal);
        var identityProvider = authOptions.PublicIdentityProvider;
        var account = await accountsDbContext.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(current =>
                current.IdentityProvider == identityProvider &&
                current.IdentitySubject == identity.Subject,
                cancellationToken);

        if (account is null)
        {
            return Results.Ok(new CurrentUserResponseDto(
                identity.Subject,
                identity.Email,
                identity.EmailVerified,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                AccountRequired: true,
                ProfileRequired: true,
                OnboardingRequired: true));
        }

        var profile = await profilesDbContext.Profiles
            .AsNoTracking()
            .SingleOrDefaultAsync(current => current.AccountId == account.Id, cancellationToken);

        return Results.Ok(ToResponse(identity, account.Id, account.Status.ToString(), profile));
    }

    private static CurrentUserResponseDto ToResponse(
        PublicIdentity identity,
        Guid accountId,
        string accountStatus,
        Profile? profile)
    {
        return new CurrentUserResponseDto(
            identity.Subject,
            identity.Email,
            identity.EmailVerified,
            accountId,
            accountStatus,
            profile?.Id,
            profile?.Handle,
            profile?.DisplayName,
            profile?.Bio,
            profile?.ProfilePictureUrl,
            AccountRequired: false,
            ProfileRequired: profile is null,
            OnboardingRequired: profile is null || profile.OnboardingCompletedAt is null);
    }

    private static PublicIdentity GetIdentity(ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Authenticated public token does not contain a subject.");
        var email = principal.FindFirstValue("email")
            ?? throw new InvalidOperationException("Authenticated public token does not contain an email.");
        var emailVerified = string.Equals(
            principal.FindFirstValue("email_verified"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        return new PublicIdentity(subject, email, emailVerified);
    }

    private sealed record PublicIdentity(
        string Subject,
        string Email,
        bool EmailVerified);
}
