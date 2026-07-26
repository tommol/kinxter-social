using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Kinxter.Media.Application;

internal static class S3RequestSigner
{
    public static string PresignPut(MediaStorageOptions options, string objectKey, DateTimeOffset now, TimeSpan lifetime, string? endpoint = null)
        => Presign(options, objectKey, "PUT", now, lifetime, endpoint);

    public static string PresignGet(MediaStorageOptions options, string objectKey, DateTimeOffset now, TimeSpan lifetime, string? endpoint = null)
        => Presign(options, objectKey, "GET", now, lifetime, endpoint);

    private static string Presign(MediaStorageOptions options, string objectKey, string method, DateTimeOffset now, TimeSpan lifetime, string? endpointOverride)
    {
        var endpoint = new Uri((endpointOverride ?? options.Endpoint).TrimEnd('/'));
        var date = now.UtcDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var timestamp = now.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var scope = $"{date}/{options.Region}/s3/aws4_request";
        var path = $"/{Uri.EscapeDataString(options.Bucket)}/{string.Join('/', objectKey.Split('/').Select(Uri.EscapeDataString))}";
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["X-Amz-Algorithm"] = "AWS4-HMAC-SHA256",
            ["X-Amz-Credential"] = $"{options.AccessKey}/{scope}",
            ["X-Amz-Date"] = timestamp,
            ["X-Amz-Expires"] = ((int)lifetime.TotalSeconds).ToString(CultureInfo.InvariantCulture),
            ["X-Amz-SignedHeaders"] = "host"
        };
        var query = string.Join('&', parameters.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        var canonical = $"{method}\n{path}\n{query}\nhost:{endpoint.Authority}\n\nhost\nUNSIGNED-PAYLOAD";
        var stringToSign = $"AWS4-HMAC-SHA256\n{timestamp}\n{scope}\n{Hex(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))}";
        var signature = Hex(Hmac(SigningKey(options.SecretKey, date, options.Region), stringToSign));
        return $"{endpoint.Scheme}://{endpoint.Authority}{path}?{query}&X-Amz-Signature={signature}";
    }
    private static byte[] SigningKey(string secret, string date, string region) => Hmac(Hmac(Hmac(Hmac(Encoding.UTF8.GetBytes("AWS4" + secret), date), region), "s3"), "aws4_request");
    private static byte[] Hmac(byte[] key, string value) => HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(value));
    private static string Hex(byte[] value) => Convert.ToHexStringLower(value);
}
