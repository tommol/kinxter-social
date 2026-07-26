using Kinxter.Shared.Abstractions.Application;

namespace Kinxter.Profiles.Application.CreateCurrentProfile;

public sealed record CreateCurrentProfileCommand(
    string IdentityProvider,
    string IdentitySubject,
    string Handle,
    string DisplayName,
    string? Bio,
    Guid? AvatarAssetId) : ICommand<CreateCurrentProfileResult>;
