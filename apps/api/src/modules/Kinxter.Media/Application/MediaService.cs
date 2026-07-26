using Kinxter.Media.Contracts;
using Kinxter.Media.Infrastructure;
using Kinxter.Media.Model;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Kinxter.Media.Application;

internal sealed class MediaService(
    MediaDbContext dbContext,
    MediaStorageOptions options,
    IClock clock,
    IHttpClientFactory httpClientFactory) : IMediaService
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };
    private const long MaxBytes = 5 * 1024 * 1024;
    public async Task<AvatarUpload> CreateAvatarUploadAsync(Guid accountId, string contentType, long size, CancellationToken cancellationToken = default)
    {
        if (!AllowedTypes.Contains(contentType) || size is <= 0 or > MaxBytes) throw new ArgumentException("Avatar must be a JPEG, PNG or WebP image up to 5 MB.");
        var now = clock.UtcNow; var id = Guid.CreateVersion7(now); var extension = contentType.ToLowerInvariant() switch { "image/jpeg" => "jpg", "image/png" => "png", _ => "webp" }; var key = $"avatars/{accountId:N}/{id:N}.{extension}";
        dbContext.Assets.Add(new MediaAsset(id, accountId, key, contentType.ToLowerInvariant(), size, now)); await dbContext.SaveChangesAsync(cancellationToken);
        var expires = now.AddMinutes(10); return new AvatarUpload(id, S3RequestSigner.PresignPut(options, key, now, TimeSpan.FromMinutes(10), options.BrowserEndpoint), expires);
    }
    public async Task<bool> CompleteAvatarUploadAsync(Guid accountId, Guid assetId, CancellationToken cancellationToken = default)
    {
        var asset = await dbContext.Assets.SingleOrDefaultAsync(current => current.Id == assetId && current.AccountId == accountId, cancellationToken); if (asset is null) return false;
        if (asset.Status == MediaAssetStatus.Ready) return true;
        if (asset.Status == MediaAssetStatus.Rejected || asset.DeclaredSize > MaxBytes || !AllowedTypes.Contains(asset.ContentType)) return false;

        var client = httpClientFactory.CreateClient("media-storage");
        using var sourceResponse = await client.GetAsync(
            S3RequestSigner.PresignGet(options, asset.ObjectKey, clock.UtcNow, TimeSpan.FromMinutes(2), options.ServiceEndpoint),
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        sourceResponse.EnsureSuccessStatusCode();

        if (sourceResponse.Content.Headers.ContentLength is > MaxBytes)
        {
            return await RejectAsync(asset, cancellationToken);
        }

        await using var source = new MemoryStream();
        await sourceResponse.Content.CopyToAsync(source, cancellationToken);
        if (source.Length is <= 0 or > MaxBytes)
        {
            return await RejectAsync(asset, cancellationToken);
        }

        var bytes = source.ToArray();
        var format = Image.DetectFormat(bytes);
        if (format is null ||
            !AllowedTypes.Contains(format.DefaultMimeType) ||
            !string.Equals(format.DefaultMimeType, asset.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            return await RejectAsync(asset, cancellationToken);
        }

        try
        {
            using var image = Image.Load(bytes);
            image.Mutate(operation => operation
                .AutoOrient()
                .Resize(new ResizeOptions
                {
                    Size = new Size(512, 512),
                    Mode = ResizeMode.Crop,
                    Position = AnchorPositionMode.Center
                }));
            image.Metadata.ExifProfile = null;

            await using var normalized = new MemoryStream();
            if (format.DefaultMimeType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
            {
                await image.SaveAsync(normalized, new JpegEncoder { Quality = 85 }, cancellationToken);
            }
            else if (format.DefaultMimeType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
            {
                await image.SaveAsync(normalized, new PngEncoder(), cancellationToken);
            }
            else
            {
                await image.SaveAsync(normalized, new WebpEncoder { Quality = 85 }, cancellationToken);
            }

            if (normalized.Length > MaxBytes)
            {
                return await RejectAsync(asset, cancellationToken);
            }

            normalized.Position = 0;
            using var uploadContent = new StreamContent(normalized);
            uploadContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(format.DefaultMimeType);
            using var normalizedResponse = await client.PutAsync(
                S3RequestSigner.PresignPut(options, asset.ObjectKey, clock.UtcNow, TimeSpan.FromMinutes(2), options.ServiceEndpoint),
                uploadContent,
                cancellationToken);
            normalizedResponse.EnsureSuccessStatusCode();

            asset.Complete(normalized.Length, clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (UnknownImageFormatException)
        {
            return await RejectAsync(asset, cancellationToken);
        }
    }

    private async Task<bool> RejectAsync(MediaAsset asset, CancellationToken cancellationToken)
    {
        asset.Reject(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return false;
    }
    public Task<bool> IsReadyAndOwnedAsync(Guid accountId, Guid assetId, CancellationToken cancellationToken = default) => dbContext.Assets.AsNoTracking().AnyAsync(asset => asset.Id == assetId && asset.AccountId == accountId && asset.Status == MediaAssetStatus.Ready, cancellationToken);
}
