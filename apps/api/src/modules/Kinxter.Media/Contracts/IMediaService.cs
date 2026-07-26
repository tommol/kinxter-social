namespace Kinxter.Media.Contracts;

public sealed record AvatarUpload(Guid AssetId, string UploadUrl, DateTimeOffset ExpiresAt);
public interface IMediaService
{
    Task<AvatarUpload> CreateAvatarUploadAsync(Guid accountId, string contentType, long size, CancellationToken cancellationToken = default);
    Task<bool> CompleteAvatarUploadAsync(Guid accountId, Guid assetId, CancellationToken cancellationToken = default);
    Task<bool> IsReadyAndOwnedAsync(Guid accountId, Guid assetId, CancellationToken cancellationToken = default);
}
