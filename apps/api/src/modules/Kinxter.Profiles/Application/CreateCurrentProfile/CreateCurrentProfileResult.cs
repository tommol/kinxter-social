using Kinxter.Profiles.Model;

namespace Kinxter.Profiles.Application.CreateCurrentProfile;

public sealed record CreateCurrentProfileResult(
    CreateCurrentProfileStatus Status,
    Profile? Profile)
{
    public static CreateCurrentProfileResult Success(CreateCurrentProfileStatus status, Profile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new CreateCurrentProfileResult(status, profile);
    }

    public static CreateCurrentProfileResult Failure(CreateCurrentProfileStatus status)
    {
        return new CreateCurrentProfileResult(status, null);
    }
}
