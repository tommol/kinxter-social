using Kinxter.SocialGraph.Model;
using Xunit;

namespace Kinxter.UnitTests.SocialGraph;

public sealed class FollowTests
{
    [Fact]
    public void Repeated_follow_preserves_an_existing_accepted_relationship()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var follow = new Follow(Guid.NewGuid(), Guid.NewGuid(), FollowStatus.Accepted, createdAt);

        follow.Request(FollowStatus.Pending, createdAt.AddMinutes(1));

        Assert.Equal(FollowStatus.Accepted, follow.Status);
        Assert.Equal(createdAt, follow.AcceptedAt);
        Assert.Null(follow.UpdatedAt);
    }

    [Fact]
    public void Follow_rejects_self_reference()
    {
        var profileId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() =>
            new Follow(profileId, profileId, FollowStatus.Pending, DateTimeOffset.UtcNow));
    }
}
