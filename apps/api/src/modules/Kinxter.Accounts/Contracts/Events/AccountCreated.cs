using Kinxter.Shared.Abstractions.Events;

namespace Kinxter.Accounts.Contracts.Events;

public sealed record AccountCreated(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid AccountId,
    string Handle,
    string DisplayName) : ModuleEvent(EventId, OccurredAt);
