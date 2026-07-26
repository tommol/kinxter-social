using Microsoft.Extensions.Configuration;

namespace Kinxter.Media;

public sealed class MediaStorageOptions
{
    public const string SectionName = "Media:S3";
    public string Endpoint { get; init; } = "http://localhost:9000";
    public string? PublicEndpoint { get; init; }
    public string? InternalEndpoint { get; init; }
    public string AccessKey { get; init; } = "kinxter";
    public string SecretKey { get; init; } = "kinxter-media-secret";
    public string Bucket { get; init; } = "kinxter-media";
    public string Region { get; init; } = "us-east-1";
    public string BrowserEndpoint => PublicEndpoint ?? Endpoint;
    public string ServiceEndpoint => InternalEndpoint ?? Endpoint;
    public static MediaStorageOptions FromConfiguration(IConfiguration configuration) => configuration.GetSection(SectionName).Get<MediaStorageOptions>() ?? new();
}
