using Kinxter.Accounts.Infrastructure.Persistence;
using Kinxter.Api.Authentication;
using Kinxter.Api.Contracts.Dtos;
using Kinxter.Profiles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Api;

internal static class MonitoringEndpoints
{
    private const string StatusOk = "ok";
    private const string StatusDegraded = "degraded";
    private const string StatusDown = "down";

    public static IEndpointRouteBuilder MapMonitoringEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/monitoring")
            .WithTags("Monitoring")
            .RequireAuthorization(ApiAuthorizationPolicies.BackofficeAdmin);

        group.MapGet("/overview", GetOverviewAsync)
            .WithName("GetMonitoringOverview")
            .WithSummary("Returns operational monitoring data for the application.")
            .Produces<MonitoringOverviewResponseDto>();

        return app;
    }

    private static async Task<MonitoringOverviewResponseDto> GetOverviewAsync(
        AccountsDbContext accountsDbContext,
        ProfilesDbContext profilesDbContext,
        CancellationToken cancellationToken)
    {
        var checkedAt = DateTimeOffset.UtcNow;
        var dependencies = new[]
        {
            await CheckDatabaseAsync("accounts-db", accountsDbContext, cancellationToken),
            await CheckDatabaseAsync("profiles-db", profilesDbContext, cancellationToken)
        };

        var metrics = new MonitoringMetricsDto(
            await QueryOrDefaultAsync(
                () => accountsDbContext.Accounts.AsNoTracking().LongCountAsync(cancellationToken),
                0),
            await QueryOrDefaultAsync(
                () => profilesDbContext.Profiles.AsNoTracking().LongCountAsync(cancellationToken),
                0));

        var outbox = new[]
        {
            await GetAccountsOutboxAsync(accountsDbContext, cancellationToken)
        };

        var status = dependencies.Any(dependency => dependency.Status == StatusDown)
            ? StatusDown
            : outbox.Any(module => module.FailedCount > 0)
                ? StatusDegraded
                : StatusOk;

        return new MonitoringOverviewResponseDto(
            "Kinxter.Api",
            status,
            checkedAt,
            dependencies,
            metrics,
            outbox);
    }

    private static async Task<MonitoringDependencyDto> CheckDatabaseAsync(
        string name,
        DbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? new MonitoringDependencyDto(name, StatusOk)
                : new MonitoringDependencyDto(name, StatusDown, "Database connection was rejected.");
        }
        catch (Exception exception)
        {
            return new MonitoringDependencyDto(name, StatusDown, exception.Message);
        }
    }

    private static async Task<MonitoringOutboxDto> GetAccountsOutboxAsync(
        AccountsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var pendingMessages = dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message => message.ProcessedAt == null);

        var failedMessages = pendingMessages
            .Where(message => message.Error != null);

        var pendingCount = await QueryOrDefaultAsync(
            () => pendingMessages.LongCountAsync(cancellationToken),
            0);
        var failedCount = await QueryOrDefaultAsync(
            () => failedMessages.LongCountAsync(cancellationToken),
            0);

        return new MonitoringOutboxDto(
            "accounts",
            pendingCount,
            failedCount,
            pendingCount > 0
                ? await QueryOrDefaultAsync(
                    () => pendingMessages.MinAsync(message => (DateTimeOffset?)message.CreatedAt, cancellationToken),
                    null)
                : null,
            failedCount > 0
                ? await QueryOrDefaultAsync(
                    () => failedMessages.MaxAsync(message => message.LastAttemptedAt, cancellationToken),
                    null)
                : null);
    }

    private static async Task<T> QueryOrDefaultAsync<T>(
        Func<Task<T>> query,
        T fallback)
    {
        try
        {
            return await query();
        }
        catch
        {
            return fallback;
        }
    }
}
