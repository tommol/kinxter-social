namespace Kinxter.Media.Model;

public enum MediaAssetStatus { Pending = 1, Ready = 2, Rejected = 3 }

public sealed class MediaAsset
{
    private MediaAsset() { ObjectKey = ContentType = null!; }
    public MediaAsset(Guid id, Guid accountId, string objectKey, string contentType, long declaredSize, DateTimeOffset createdAt)
    { Id = id; AccountId = accountId; ObjectKey = objectKey; ContentType = contentType; DeclaredSize = declaredSize; Status = MediaAssetStatus.Pending; CreatedAt = createdAt; }
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public string ObjectKey { get; private set; }
    public string ContentType { get; private set; }
    public long DeclaredSize { get; private set; }
    public long? ActualSize { get; private set; }
    public MediaAssetStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public void Complete(long actualSize, DateTimeOffset at) { ActualSize = actualSize; Status = MediaAssetStatus.Ready; CompletedAt = at; }
    public void Reject(DateTimeOffset at) { Status = MediaAssetStatus.Rejected; CompletedAt = at; }
}
