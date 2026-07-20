using System.Security.Claims;
using Kinxter.Accounts.Contracts.Events;
using Kinxter.Accounts.Infrastructure.Outbox;
using Kinxter.Accounts.Infrastructure.Persistence;
using Kinxter.Accounts.Model;
using Kinxter.Api.Authentication;
using Kinxter.Api.Contracts.Dtos;
using Kinxter.Profiles.Infrastructure.Persistence;
using Kinxter.Profiles.Model;
using Kinxter.Shared.Abstractions.Outbox;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Api;

internal static class OnboardingEndpoints
{
    public static IEndpointRouteBuilder MapOnboardingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("")
            .WithTags("Onboarding")
            .RequireAuthorization(ApiAuthorizationPolicies.PublicUser);

        group.MapGet("/me", GetCurrentUserAsync)
            .WithName("GetCurrentUser")
            .WithSummary("Returns the current public user's account and profile state.")
            .Produces<CurrentUserResponseDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/onboarding/account", OnboardAccountAsync)
            .WithName("OnboardAccount")
            .WithSummary("Creates the public user's account/profile after identity sign-in.")
            .Produces<CurrentUserResponseDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

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
                OnboardingRequired: true));
        }

        var profile = await profilesDbContext.Profiles
            .AsNoTracking()
            .SingleOrDefaultAsync(current => current.AccountId == account.Id, cancellationToken);

        return Results.Ok(ToResponse(identity, account, profile));
    }

    private static async Task<IResult> OnboardAccountAsync(
        OnboardAccountRequestDto request,
        ClaimsPrincipal principal,
        AccountsDbContext accountsDbContext,
        ProfilesDbContext profilesDbContext,
        IOutboxWriter<AccountsOutbox> outboxWriter,
        IClock clock,
        ApiAuthenticationOptions authOptions,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Handle) || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Handle)] = ["Handle is required."],
                [nameof(request.DisplayName)] = ["Display name is required."]
            });
        }

        var identity = GetIdentity(principal);
        var identityProvider = authOptions.PublicIdentityProvider;
        var normalizedHandle = request.Handle.Trim().ToLowerInvariant();
        var existingProfileWithHandle = await profilesDbContext.Profiles
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.NormalizedHandle == normalizedHandle, cancellationToken);
        var existingAccount = await accountsDbContext.Accounts
            .SingleOrDefaultAsync(current =>
                current.IdentityProvider == identityProvider &&
                current.IdentitySubject == identity.Subject,
                cancellationToken);

        if (existingProfileWithHandle is not null &&
            (existingAccount is null || existingProfileWithHandle.AccountId != existingAccount.Id))
        {
            return Results.Conflict(new { error = "Handle is already taken." });
        }

        if (existingAccount is not null)
        {
            var profile = await EnsureProfileAsync(
                profilesDbContext,
                existingAccount.Id,
                request,
                clock.UtcNow,
                cancellationToken);

            return Results.Ok(ToResponse(identity, existingAccount, profile));
        }

        var normalizedEmail = identity.Email.Trim().ToLowerInvariant();
        var emailExists = await accountsDbContext.Accounts
            .AsNoTracking()
            .AnyAsync(account => account.NormalizedEmail == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            return Results.Conflict(new { error = "Email is already connected to another account." });
        }

        var now = clock.UtcNow;
        var account = Account.Create(
            Guid.CreateVersion7(now),
            identity.Email,
            identityProvider,
            identity.Subject,
            identity.EmailVerified,
            now);
        var accountCreated = new AccountCreated(
            Guid.CreateVersion7(now),
            now,
            account.Id,
            request.Handle,
            request.DisplayName);

        accountsDbContext.Accounts.Add(account);
        await outboxWriter.AddAsync(accountCreated, cancellationToken);
        await accountsDbContext.SaveChangesAsync(cancellationToken);

        var createdProfile = await EnsureProfileAsync(
            profilesDbContext,
            account.Id,
            request,
            now,
            cancellationToken);

        return Results.Created($"/api/v1/accounts/{account.Id}", ToResponse(identity, account, createdProfile));
    }

    private static async Task<Profile> EnsureProfileAsync(
        ProfilesDbContext profilesDbContext,
        Guid accountId,
        OnboardAccountRequestDto request,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        var profile = await profilesDbContext.Profiles
            .SingleOrDefaultAsync(current => current.AccountId == accountId, cancellationToken);

        if (profile is not null)
        {
            return profile;
        }

        profile = Profile.Create(
            Guid.CreateVersion7(createdAt),
            accountId,
            request.Handle,
            request.DisplayName,
            createdAt);

        profilesDbContext.Profiles.Add(profile);
        await profilesDbContext.SaveChangesAsync(cancellationToken);

        return profile;
    }

    private static CurrentUserResponseDto ToResponse(
        PublicIdentity identity,
        Account account,
        Profile? profile)
    {
        return new CurrentUserResponseDto(
            identity.Subject,
            identity.Email,
            identity.EmailVerified,
            account.Id,
            account.Status.ToString(),
            profile?.Handle,
            profile?.DisplayName,
            OnboardingRequired: profile is null);
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
