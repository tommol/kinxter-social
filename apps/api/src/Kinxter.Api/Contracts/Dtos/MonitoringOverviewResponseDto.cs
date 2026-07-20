namespace Kinxter.Api.Contracts.Dtos;

public sealed record MonitoringOverviewResponseDto(
    string Service,
    string Status,
    DateTimeOffset CheckedAt,
    IReadOnlyCollection<MonitoringDependencyDto> Dependencies,
    MonitoringMetricsDto Metrics,
    IReadOnlyCollection<MonitoringOutboxDto> Outbox);

public sealed record MonitoringDependencyDto(
    string Name,
    string Status,
    string? Detail = null);

public sealed record MonitoringMetricsDto(
    long AccountCount,
    long ProfileCount);

public sealed record MonitoringOutboxDto(
    string ModuleName,
    long PendingCount,
    long FailedCount,
    DateTimeOffset? OldestPendingCreatedAt,
    DateTimeOffset? LastFailureAt);
