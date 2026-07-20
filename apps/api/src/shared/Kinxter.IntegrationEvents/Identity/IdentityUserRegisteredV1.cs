using Kinxter.Shared.Abstractions.Events;

namespace Kinxter.IntegrationEvents.Identity;

[ModuleEventName("identity.user-registered.v1")]
public sealed record IdentityUserRegisteredV1(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string Realm,
    string Subject,
    string Email,
    bool EmailVerified) : ModuleEvent(EventId, OccurredAt);
