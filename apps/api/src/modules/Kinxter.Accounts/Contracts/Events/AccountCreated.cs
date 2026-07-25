using Kinxter.Shared.Abstractions.Events;

namespace Kinxter.Accounts.Contracts.Events;

[ModuleEventName("accounts.account-created.v1")]
public sealed record AccountCreated(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid AccountId) : ModuleEvent(EventId, OccurredAt);
