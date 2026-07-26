using System.Security.Claims;
using Kinxter.Accounts;
using Kinxter.Accounts.Contracts;
using Kinxter.Accounts.Model;
using Kinxter.Onboarding.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Kinxter.Onboarding.Api;

public static class OnboardingEndpoints
{
    public static IEndpointRouteBuilder MapOnboardingEndpoints(this IEndpointRouteBuilder app, string policy)
    {
        var group = app.MapGroup("/onboarding").WithTags("Onboarding").RequireAuthorization(policy);
        group.MapGet("", GetAsync).WithName("GetOnboardingState");
        group.MapPut("/consents", AcceptConsentsAsync).WithName("AcceptOnboardingConsents");
        group.MapPost("/steps/{step}/complete", CompleteStepAsync).WithName("CompleteOptionalOnboardingStep");
        group.MapPost("/steps/{step}/skip", SkipStepAsync).WithName("SkipOptionalOnboardingStep");
        group.MapPost("/complete", CompleteAsync).WithName("CompleteOnboarding");
        return app;
    }
    private static async Task<IResult> GetAsync(ClaimsPrincipal principal, IAccountsService accounts, IOnboardingService onboarding, CancellationToken token) { var account = await CurrentAsync(principal, accounts, token); return !IsReady(account) ? Results.Conflict(new { error = "Account is not initialized yet." }) : Results.Ok(await onboarding.GetAsync(account!.AccountId, token)); }
    private static async Task<IResult> AcceptConsentsAsync(AcceptConsentsRequest request, ClaimsPrincipal principal, IAccountsService accounts, AccountConsentOptions options, CancellationToken token)
    { var account = await CurrentAsync(principal, accounts, token); if (!IsReady(account)) return Results.Conflict(new { error = "Account is not initialized yet." }); var status = await accounts.AcceptConsentsAsync(account!.AccountId, request.AdultConfirmed, request.TermsVersion, request.PrivacyVersion, request.Locale, token); return status == AcceptConsentsStatus.Accepted ? Results.NoContent() : status == AcceptConsentsStatus.AccountNotActive ? Results.Conflict(new { error = status.ToString() }) : Results.ValidationProblem(new Dictionary<string, string[]> { ["consents"] = [status.ToString()] }); }
    private static Task<IResult> CompleteStepAsync(string step, ClaimsPrincipal principal, IAccountsService accounts, IOnboardingService onboarding, CancellationToken token) => SetStepAsync(step, false, principal, accounts, onboarding, token);
    private static Task<IResult> SkipStepAsync(string step, ClaimsPrincipal principal, IAccountsService accounts, IOnboardingService onboarding, CancellationToken token) => SetStepAsync(step, true, principal, accounts, onboarding, token);
    private static async Task<IResult> SetStepAsync(string step, bool skipped, ClaimsPrincipal principal, IAccountsService accounts, IOnboardingService onboarding, CancellationToken token) { if (step is not ("interests" or "recommendations")) return Results.NotFound(); var account = await CurrentAsync(principal, accounts, token); if (!IsReady(account)) return Results.Conflict(); return await onboarding.SetOptionalStepAsync(account!.AccountId, step, skipped, token) ? Results.NoContent() : Results.Conflict(new { error = "Onboarding steps must be completed in order." }); }
    private static async Task<IResult> CompleteAsync(ClaimsPrincipal principal, IAccountsService accounts, IOnboardingService onboarding, CancellationToken token) { var account = await CurrentAsync(principal, accounts, token); if (!IsReady(account)) return Results.Conflict(); return await onboarding.CompleteAsync(account!.AccountId, token) ? Results.Ok(await onboarding.GetAsync(account.AccountId, token)) : Results.Conflict(new { error = "Required onboarding steps are incomplete." }); }
    private static Task<AccountState?> CurrentAsync(ClaimsPrincipal principal, IAccountsService accounts, CancellationToken token) => accounts.GetByIdentityAsync(principal.FindFirstValue("realm")!, principal.FindFirstValue("sub")!, token);
    private static bool IsReady(AccountState? account) => account?.Status == AccountStatus.Active;
    public sealed record AcceptConsentsRequest(bool AdultConfirmed, string TermsVersion, string PrivacyVersion, string Locale);
}
