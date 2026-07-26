namespace Kinxter.SocialGraph.Model;

public enum FollowStatus { Pending = 1, Accepted = 2, Rejected = 3 }

public sealed class Follow
{
    private Follow() { }
    public Follow(Guid followerProfileId, Guid followedProfileId, FollowStatus status, DateTimeOffset createdAt)
    {
        if (followerProfileId == followedProfileId) throw new ArgumentException("A profile cannot follow itself.");
        FollowerProfileId = followerProfileId; FollowedProfileId = followedProfileId; Status = status; CreatedAt = createdAt;
        if (status == FollowStatus.Accepted) AcceptedAt = createdAt;
    }
    public Guid FollowerProfileId { get; private set; }
    public Guid FollowedProfileId { get; private set; }
    public FollowStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public void Accept(DateTimeOffset at) { Status = FollowStatus.Accepted; AcceptedAt ??= at; UpdatedAt = at; }
    public void Reject(DateTimeOffset at) { Status = FollowStatus.Rejected; UpdatedAt = at; }
    public void Request(FollowStatus status, DateTimeOffset at)
    {
        if (Status == FollowStatus.Accepted)
        {
            return;
        }

        Status = status;
        if (status == FollowStatus.Accepted) AcceptedAt = at;
        UpdatedAt = at;
    }
}
