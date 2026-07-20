using Kinxter.Shared.Abstractions.Events;

namespace Kinxter.IntegrationEvents.Identity;

[ModuleEventName("identity.email-confirmed.v1")]
public sealed record IdentityEmailConfirmedV1(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string Realm,
    string Subject,
    string Email) : ModuleEvent(EventId, OccurredAt);
