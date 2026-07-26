namespace Kinxter.Communities.Model;

public enum CommunityStatus { Draft = 1, PendingReview = 2, Published = 3, Rejected = 4, Archived = 5 }

public sealed class Community
{
    private Community() { Slug = Name = Description = null!; }
    public Community(Guid id, Guid ownerProfileId, string slug, string name, string description, DateTimeOffset createdAt)
    {
        Name = Description = "";
        Id = id; OwnerProfileId = ownerProfileId; Slug = NormalizeSlug(slug); Update(name, description, createdAt); Status = CommunityStatus.Draft; CreatedAt = createdAt;
    }
    public Guid Id { get; private set; }
    public Guid OwnerProfileId { get; private set; }
    public string Slug { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public CommunityStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }

    public void Update(string name, string description, DateTimeOffset at)
    {
        if (Status is CommunityStatus.PendingReview or CommunityStatus.Archived) throw new InvalidOperationException("Community cannot be edited in its current state.");
        Name = Required(name, 120); Description = Required(description, 2000); UpdatedAt = at;
        if (Status == CommunityStatus.Rejected) { Status = CommunityStatus.Draft; RejectionReason = null; }
    }
    public void Submit(DateTimeOffset at) { if (Status != CommunityStatus.Draft) throw new InvalidOperationException("Only a draft can be submitted."); Status = CommunityStatus.PendingReview; UpdatedAt = at; }
    public void Publish(DateTimeOffset at) { if (Status != CommunityStatus.PendingReview) throw new InvalidOperationException("Only pending community can be published."); Status = CommunityStatus.Published; PublishedAt = at; UpdatedAt = at; RejectionReason = null; }
    public void Reject(string reason, DateTimeOffset at) { if (Status != CommunityStatus.PendingReview) throw new InvalidOperationException("Only pending community can be rejected."); Status = CommunityStatus.Rejected; RejectionReason = Required(reason, 1000); UpdatedAt = at; }
    private static string NormalizeSlug(string value) { var slug = Required(value, 80).ToLowerInvariant(); if (slug.Any(c => !(c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-'))) throw new ArgumentException("Invalid community slug."); return slug; }
    private static string Required(string value, int max) { ArgumentException.ThrowIfNullOrWhiteSpace(value); var result = value.Trim(); return result.Length <= max ? result : throw new ArgumentException($"Value exceeds {max} characters."); }
}

public sealed class CommunityMembership
{
    private CommunityMembership() { }
    public CommunityMembership(Guid communityId, Guid profileId, bool isOwner, DateTimeOffset joinedAt) { CommunityId = communityId; ProfileId = profileId; IsOwner = isOwner; JoinedAt = joinedAt; }
    public Guid CommunityId { get; private set; }
    public Guid ProfileId { get; private set; }
    public bool IsOwner { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }
}
