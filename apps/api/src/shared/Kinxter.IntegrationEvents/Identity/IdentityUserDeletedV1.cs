using Kinxter.Shared.Abstractions.Events;

namespace Kinxter.IntegrationEvents.Identity;

[ModuleEventName("identity.user-deleted.v1")]
public sealed record IdentityUserDeletedV1(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string Realm,
    string Subject) : ModuleEvent(EventId, OccurredAt);
