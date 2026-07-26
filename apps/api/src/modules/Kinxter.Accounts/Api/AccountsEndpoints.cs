using System.Security.Claims;
using Kinxter.Accounts.Contracts.Dtos;
using Kinxter.Accounts.Infrastructure.Persistence;
using Kinxter.Accounts.Model;
using Kinxter.IntegrationEvents.Identity;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Accounts.Api;

public static class AccountsEndpoints
{
    public static IEndpointRouteBuilder MapAccountsEndpoints(this IEndpointRouteBuilder app, string publicUserPolicy)
    {
        var group = app.MapGroup("/accounts")
            .WithTags("Accounts")
            .RequireAuthorization(publicUserPolicy);

        group.MapGet("/me/consents", GetConsentsAsync)
            .WithName("GetCurrentAccountConsents")
            .Produces<AccountConsentsResponseDto>();
        group.MapPut("/me/consents", AcceptConsentsAsync)
            .WithName("AcceptCurrentAccountConsents")
            .Produces<AccountConsentsResponseDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> GetConsentsAsync(
        ClaimsPrincipal principal,
        AccountsDbContext dbContext,
        AccountConsentOptions options,
        CancellationToken cancellationToken)
    {
        var accountId = await FindCurrentAccountIdAsync(principal, dbContext, cancellationToken);
        var consent = accountId == Guid.Empty
            ? null
            : await dbContext.AccountConsents.AsNoTracking()
                .Where(current => current.AccountId == accountId &&
                    current.TermsVersion == options.TermsVersion &&
                    current.PrivacyVersion == options.PrivacyVersion)
                .OrderByDescending(current => current.AcceptedAt)
                .FirstOrDefaultAsync(cancellationToken);

        return Results.Ok(ToResponse(consent, options));
    }

    private static async Task<IResult> AcceptConsentsAsync(
        AcceptAccountConsentsRequestDto request,
        ClaimsPrincipal principal,
        AccountsDbContext dbContext,
        AccountConsentOptions options,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (!request.AdultConfirmed)
        {
            errors[nameof(request.AdultConfirmed)] = ["Adult confirmation is required."];
        }

        if (!string.Equals(request.TermsVersion, options.TermsVersion, StringComparison.Ordinal))
        {
            errors[nameof(request.TermsVersion)] = ["The terms version is no longer current."];
        }

        if (!string.Equals(request.PrivacyVersion, options.PrivacyVersion, StringComparison.Ordinal))
        {
            errors[nameof(request.PrivacyVersion)] = ["The privacy policy version is no longer current."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var account = await FindCurrentAccountAsync(principal, dbContext, cancellationToken);

        if (account is null || account.Status != AccountStatus.Active)
        {
            return Results.Conflict(new { error = "The active account is not initialized yet." });
        }

        var existing = await dbContext.AccountConsents.SingleOrDefaultAsync(current =>
            current.AccountId == account.Id &&
            current.TermsVersion == options.TermsVersion &&
            current.PrivacyVersion == options.PrivacyVersion,
            cancellationToken);

        if (existing is null)
        {
            existing = new AccountConsent(
                Guid.CreateVersion7(clock.UtcNow),
                account.Id,
                options.TermsVersion,
                options.PrivacyVersion,
                request.Locale,
                clock.UtcNow);
            dbContext.AccountConsents.Add(existing);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Results.Ok(ToResponse(existing, options));
    }

    internal static Task<Guid> FindCurrentAccountIdAsync(
        ClaimsPrincipal principal,
        AccountsDbContext dbContext,
        CancellationToken cancellationToken) =>
        CurrentAccountQuery(principal, dbContext)
            .Select(account => account.Id)
            .SingleOrDefaultAsync(cancellationToken);

    internal static Task<Account?> FindCurrentAccountAsync(
        ClaimsPrincipal principal,
        AccountsDbContext dbContext,
        CancellationToken cancellationToken) =>
        CurrentAccountQuery(principal, dbContext).SingleOrDefaultAsync(cancellationToken);

    private static IQueryable<Account> CurrentAccountQuery(ClaimsPrincipal principal, AccountsDbContext dbContext)
    {
        var subject = principal.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Authenticated token does not contain a subject.");
        var realm = principal.FindFirstValue("realm")
            ?? throw new InvalidOperationException("Authenticated token does not contain a realm.");
        var identityProvider = KinxterAuthIdentityProvider.ForRealm(realm);

        return dbContext.Accounts.Where(account =>
            account.IdentityProvider == identityProvider && account.IdentitySubject == subject);
    }

    private static AccountConsentsResponseDto ToResponse(AccountConsent? consent, AccountConsentOptions options) =>
        new(
            consent is not null,
            consent?.AdultConfirmed ?? false,
            options.TermsVersion,
            options.PrivacyVersion,
            consent?.AcceptedAt);
}
