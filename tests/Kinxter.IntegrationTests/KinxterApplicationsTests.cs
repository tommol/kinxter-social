using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Kinxter.IntegrationTests;

[Collection(DockerComposeCollection.Name)]
public sealed class KinxterApplicationsTests : IDisposable
{
    private readonly ComposeEnvironmentFixture _environment;
    private readonly HttpClient _httpClient = new();

    public KinxterApplicationsTests(ComposeEnvironmentFixture environment)
    {
        _environment = environment;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    [Fact]
    public async Task Api_health_endpoint_returns_the_expected_service_status()
    {
        using var response = await _httpClient.GetAsync($"{_environment.ApiBaseUrl}/health");
        using var document = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("Kinxter.Api", document.RootElement.GetProperty("service").GetString());
    }

    [Fact]
    public async Task Auth_realms_expose_distinct_openid_connect_discovery_documents()
    {
        using var publicResponse = await _httpClient.GetAsync($"{_environment.AuthPublicBaseUrl}/.well-known/openid-configuration");
        using var backofficeResponse = await _httpClient.GetAsync($"{_environment.AuthBackofficeBaseUrl}/.well-known/openid-configuration");
        using var publicDocument = await ReadJsonAsync(publicResponse);
        using var backofficeDocument = await ReadJsonAsync(backofficeResponse);

        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, backofficeResponse.StatusCode);
        Assert.Equal(_environment.AuthPublicBaseUrl, publicDocument.RootElement.GetProperty("issuer").GetString());
        Assert.Equal(_environment.AuthBackofficeBaseUrl, backofficeDocument.RootElement.GetProperty("issuer").GetString());
        Assert.NotEqual(
            publicDocument.RootElement.GetProperty("issuer").GetString(),
            backofficeDocument.RootElement.GetProperty("issuer").GetString());
    }

    [Fact]
    public async Task Api_exposes_openapi_document_for_rest_endpoints()
    {
        using var response = await _httpClient.GetAsync($"{_environment.ApiBaseUrl}/openapi/v1.json");
        using var document = await ReadJsonAsync(response);
        var paths = document.RootElement.GetProperty("paths");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("3.", document.RootElement.GetProperty("openapi").GetString());
        Assert.True(paths.TryGetProperty("/health", out _));
        Assert.True(paths.TryGetProperty("/api/v1/me", out var me) && me.TryGetProperty("get", out _));
        Assert.True(paths.TryGetProperty("/api/v1/profiles/me", out var profile) && profile.TryGetProperty("post", out _));
        Assert.True(paths.TryGetProperty("/api/v1/onboarding", out var onboarding) && onboarding.TryGetProperty("get", out _));
        Assert.True(paths.TryGetProperty("/api/v1/onboarding/consents", out var consents) && consents.TryGetProperty("put", out _));
        Assert.True(paths.TryGetProperty("/api/v1/onboarding/complete", out var complete) && complete.TryGetProperty("post", out _));
        Assert.True(paths.TryGetProperty("/api/v1/monitoring/overview", out var monitoring) && monitoring.TryGetProperty("get", out _));
    }

    [Fact]
    public async Task Web_application_serves_the_workspace_page()
    {
        using var response = await _httpClient.GetAsync(_environment.WebBaseUrl);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Kinxter Social", html);
        Assert.Contains("Web client workspace", html);
        Assert.Contains("Sign in with Kinxter.Auth", html);
        Assert.Contains($"{_environment.ApiBaseUrl}/health", StripHtmlComments(html));
    }

    [Fact]
    public async Task Admin_application_serves_the_monitoring_dashboard()
    {
        using var response = await _httpClient.GetAsync(_environment.AdminBaseUrl);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Kinxter Admin", html);
        Assert.Contains("Monitoring", html);
    }

    [Fact]
    public async Task Admin_monitoring_endpoint_requires_a_backoffice_session()
    {
        using var response = await _httpClient.GetAsync($"{_environment.AdminBaseUrl}/api/monitoring/health");
        using var document = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("down", document.RootElement.GetProperty("status").GetString());
        Assert.Contains("HTTP 401", document.RootElement.GetProperty("error").GetString());
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();

        return await JsonDocument.ParseAsync(stream);
    }

    private static string StripHtmlComments(string value)
    {
        return Regex.Replace(value, "<!--.*?-->", "", RegexOptions.Singleline);
    }
}
