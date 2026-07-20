using Kinxter.Shared.Abstractions.Events;

namespace Kinxter.IntegrationEvents.Identity;

[ModuleEventName("identity.user-disabled.v1")]
public sealed record IdentityUserDisabledV1(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string Realm,
    string Subject,
    string Reason) : ModuleEvent(EventId, OccurredAt);
