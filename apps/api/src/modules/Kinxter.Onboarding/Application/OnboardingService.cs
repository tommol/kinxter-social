using Kinxter.Accounts.Contracts;
using Kinxter.Onboarding.Contracts;
using Kinxter.Onboarding.Infrastructure;
using Kinxter.Onboarding.Model;
using Kinxter.Profiles.Contracts;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Onboarding.Application;

internal sealed class OnboardingService(OnboardingDbContext dbContext, IAccountsService accounts, IProfilesService profiles, IClock clock) : IOnboardingService
{
    public async Task<OnboardingState> GetAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var consent = await accounts.HasCurrentConsentsAsync(accountId, cancellationToken); var profile = await profiles.GetByAccountIdAsync(accountId, cancellationToken);
        var progress = await dbContext.Progress.AsNoTracking().SingleOrDefaultAsync(current => current.AccountId == accountId, cancellationToken);
        var interests = progress?.InterestsStatus ?? OptionalStepStatus.Pending; var recommendations = progress?.RecommendationsStatus ?? OptionalStepStatus.Pending;
        var visibility = profile?.Visibility is not null; var completedAt = progress?.CompletedAt ?? profile?.OnboardingCompletedAt;
        var current = !consent ? "consents" : profile is null ? "profile" : interests == OptionalStepStatus.Pending ? "interests" : recommendations == OptionalStepStatus.Pending ? "recommendations" : !visibility ? "visibility" : completedAt is null ? "complete" : "done";
        return new(completedAt is null ? progress is null ? "NotStarted" : "InProgress" : "Completed", current, consent, profile is not null, interests.ToString(), recommendations.ToString(), visibility, completedAt);
    }

    public async Task<bool> SetOptionalStepAsync(Guid accountId, string step, bool skipped, CancellationToken cancellationToken = default)
    {
        var state = await GetAsync(accountId, cancellationToken);
        if (!await accounts.IsActiveAsync(accountId, cancellationToken) || state.CurrentStep != step)
        {
            return false;
        }

        var progress = await GetOrCreateAsync(accountId, cancellationToken); progress.SetStep(step, skipped ? OptionalStepStatus.Skipped : OptionalStepStatus.Completed, clock.UtcNow); await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CompleteAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var state = await GetAsync(accountId, cancellationToken);
        if (!await accounts.IsActiveAsync(accountId, cancellationToken) || !state.ConsentsCompleted || !state.ProfileCompleted || !state.VisibilityCompleted || state.InterestsStatus == "Pending" || state.RecommendationsStatus == "Pending") return false;
        var progress = await GetOrCreateAsync(accountId, cancellationToken); progress.Complete(clock.UtcNow);
        if (!await profiles.MarkOnboardingCompletedAsync(accountId, cancellationToken)) return false;
        await dbContext.SaveChangesAsync(cancellationToken); return true;
    }

    private async Task<OnboardingProgress> GetOrCreateAsync(Guid accountId, CancellationToken token)
    {
        var progress = await dbContext.Progress.SingleOrDefaultAsync(current => current.AccountId == accountId, token);
        if (progress is null) { progress = new OnboardingProgress(accountId, clock.UtcNow); dbContext.Progress.Add(progress); }
        return progress;
    }
}
