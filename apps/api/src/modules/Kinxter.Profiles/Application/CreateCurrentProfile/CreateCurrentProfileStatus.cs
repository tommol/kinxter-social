namespace Kinxter.Profiles.Application.CreateCurrentProfile;

public enum CreateCurrentProfileStatus
{
    Created = 1,
    AlreadyCreated = 2,
    AccountNotInitialized = 3,
    HandleAlreadyTaken = 4,
    AvatarAssetNotReady = 5
}
