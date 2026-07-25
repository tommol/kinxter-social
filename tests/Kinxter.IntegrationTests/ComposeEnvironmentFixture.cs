using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Kinxter.IntegrationTests;

public sealed class ComposeEnvironmentFixture : IAsyncLifetime
{
    private static readonly ComposeInvocation[] ComposeCandidates =
    [
        new("docker", ["compose"], "docker compose"),
        new("podman", ["compose"], "podman compose"),
        new("docker-compose", [], "docker-compose")
    ];

    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    private ComposeInvocation? _composeInvocation;
    private Dictionary<string, string>? _composeEnvironment;

    public string ApiBaseUrl { get; private set; } = "";

    public string WebBaseUrl { get; private set; } = "";

    public string AdminBaseUrl { get; private set; } = "";

    public string AuthPublicBaseUrl { get; private set; } = "";

    public string AuthBackofficeBaseUrl { get; private set; } = "";

    private string RepositoryRoot { get; } = FindRepositoryRoot();

    private string ComposeFile => Path.Combine(RepositoryRoot, "deploy", "containers", "docker-compose.yml");

    private string ProjectName { get; } = $"kinxterintegration{Environment.ProcessId}{Guid.NewGuid():N}"[..40];

    public async Task InitializeAsync()
    {
        var apiPort = GetPort("INTEGRATION_API_HTTP_PORT");
        var webPort = GetPort("INTEGRATION_WEB_HTTP_PORT");
        var adminPort = GetPort("INTEGRATION_ADMIN_HTTP_PORT");
        var authPort = GetPort("INTEGRATION_AUTH_HTTP_PORT");
        var postgresPort = GetPort("INTEGRATION_POSTGRES_PORT");
        var natsClientPort = GetPort("INTEGRATION_NATS_CLIENT_PORT");
        var natsMonitorPort = GetPort("INTEGRATION_NATS_MONITOR_PORT");
        var authPublicRealm = Environment.GetEnvironmentVariable("INTEGRATION_AUTH_PUBLIC_REALM") ?? "public";
        var authBackofficeRealm = Environment.GetEnvironmentVariable("INTEGRATION_AUTH_BACKOFFICE_REALM") ?? "backoffice";

        ApiBaseUrl = $"http://localhost:{apiPort}";
        WebBaseUrl = $"http://localhost:{webPort}";
        AdminBaseUrl = $"http://localhost:{adminPort}";
        AuthPublicBaseUrl = $"http://localhost:{authPort}/realms/{authPublicRealm}";
        AuthBackofficeBaseUrl = $"http://localhost:{authPort}/realms/{authBackofficeRealm}";

        _composeEnvironment = new Dictionary<string, string>
        {
            ["API_HTTP_PORT"] = apiPort.ToString(),
            ["WEB_HTTP_PORT"] = webPort.ToString(),
            ["ADMIN_HTTP_PORT"] = adminPort.ToString(),
            ["AUTH_HTTP_PORT"] = authPort.ToString(),
            ["AUTH_PUBLIC_REALM"] = authPublicRealm,
            ["AUTH_BACKOFFICE_REALM"] = authBackofficeRealm,
            ["AUTH_PUBLIC_PATH_BASE"] = $"/realms/{authPublicRealm}",
            ["AUTH_BACKOFFICE_PATH_BASE"] = $"/realms/{authBackofficeRealm}",
            ["AUTH_PUBLIC_ISSUER"] = AuthPublicBaseUrl,
            ["AUTH_BACKOFFICE_ISSUER"] = AuthBackofficeBaseUrl,
            ["POSTGRES_PORT"] = postgresPort.ToString(),
            ["NATS_CLIENT_PORT"] = natsClientPort.ToString(),
            ["NATS_MONITOR_PORT"] = natsMonitorPort.ToString(),
            ["ADMIN_API_BASE_URL"] = "http://api:8080",
            ["NEXT_PUBLIC_API_BASE_URL"] = ApiBaseUrl,
            ["WEB_PUBLIC_ORIGIN"] = WebBaseUrl,
            ["ADMIN_PUBLIC_ORIGIN"] = AdminBaseUrl
        };

        await RunDockerComposeAsync(["up", "--build", "--detach"]);

        await WaitForJsonAsync(
            $"{AuthPublicBaseUrl}/health",
            payload => payload.TryGetProperty("status", out var status)
                && status.GetString() == "ok"
                && payload.TryGetProperty("realm", out var realm)
                && realm.GetString() == authPublicRealm,
            TimeSpan.FromMinutes(2));

        await WaitForJsonAsync(
            $"{AuthBackofficeBaseUrl}/health",
            payload => payload.TryGetProperty("status", out var status)
                && status.GetString() == "ok"
                && payload.TryGetProperty("realm", out var realm)
                && realm.GetString() == authBackofficeRealm,
            TimeSpan.FromMinutes(2));

        await WaitForJsonAsync(
            $"{ApiBaseUrl}/health",
            payload => payload.TryGetProperty("status", out var status) && status.GetString() == "ok",
            TimeSpan.FromMinutes(2));

        await WaitForHttpOkAsync(WebBaseUrl, TimeSpan.FromMinutes(2));
        await WaitForHttpOkAsync(AdminBaseUrl, TimeSpan.FromMinutes(2));
    }

    public async Task DisposeAsync()
    {
        _httpClient.Dispose();

        if (_composeEnvironment is null)
        {
            return;
        }

        await RunDockerComposeAsync(["down", "--volumes", "--remove-orphans"], allowFailure: true);
    }

    private async Task RunDockerComposeAsync(string[] args, bool allowFailure = false)
    {
        _composeInvocation ??= await FindComposeInvocationAsync();
        var composeArgs = _composeInvocation.Args
            .Concat(["--project-name", ProjectName, "--file", ComposeFile])
            .Concat(args)
            .ToArray();

        var result = await RunProcessAsync(
            _composeInvocation.Command,
            composeArgs,
            _composeEnvironment,
            RepositoryRoot);

        if (result.ExitCode == 0 || allowFailure)
        {
            return;
        }

        throw new InvalidOperationException(
            string.Join(
                Environment.NewLine,
                new[]
                {
                    $"{_composeInvocation.Label} {string.Join(' ', args)} failed with exit code {result.ExitCode}.",
                    "Make sure Docker or Podman Compose is installed and running before executing the integration tests.",
                    result.Stdout.Trim(),
                    result.Stderr.Trim(),
                    result.Error?.Message ?? ""
                }.Where(message => !string.IsNullOrWhiteSpace(message))));
    }

    private async Task<ComposeInvocation> FindComposeInvocationAsync()
    {
        var errors = new List<string>();

        foreach (var candidate in ComposeCandidates)
        {
            var result = await RunProcessAsync(
                candidate.Command,
                [.. candidate.Args, "version"],
                environment: null,
                workingDirectory: RepositoryRoot);

            if (result.ExitCode == 0)
            {
                return candidate;
            }

            errors.Add($"{candidate.Label}: {result.Error?.Message ?? result.Stderr.Trim() ?? $"exit {result.ExitCode}"}");
        }

        throw new InvalidOperationException(
            string.Join(
                Environment.NewLine,
                new[]
                {
                    "Unable to find a working Docker Compose command.",
                    "Tried: docker compose, podman compose, docker-compose.",
                }.Concat(errors)));
    }

    private async Task WaitForHttpOkAsync(string url, TimeSpan timeout)
    {
        await WaitForAsync(
            url,
            async response => response.IsSuccessStatusCode,
            timeout);
    }

    private async Task WaitForJsonAsync(
        string url,
        Func<JsonElement, bool> validate,
        TimeSpan timeout)
    {
        await WaitForAsync(
            url,
            async response =>
            {
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                await using var stream = await response.Content.ReadAsStreamAsync();
                using var document = await JsonDocument.ParseAsync(stream);

                return validate(document.RootElement);
            },
            timeout);
    }

    private async Task WaitForAsync(
        string url,
        Func<HttpResponseMessage, Task<bool>> validate,
        TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        Exception? lastError = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using var response = await _httpClient.GetAsync(url);

                if (await validate(response))
                {
                    return;
                }

                lastError = new InvalidOperationException($"{url} returned HTTP {(int)response.StatusCode}.");
            }
            catch (Exception error)
            {
                lastError = error;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException($"Timed out waiting for {url}: {lastError?.Message ?? "no response"}");
    }

    private static async Task<ProcessResult> RunProcessAsync(
        string command,
        IReadOnlyCollection<string> args,
        IReadOnlyDictionary<string, string>? environment,
        string workingDirectory)
    {
        var startInfo = new ProcessStartInfo(command)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        if (environment is not null)
        {
            foreach (var (key, value) in environment)
            {
                startInfo.Environment[key] = value;
            }
        }

        using var process = new Process
        {
            StartInfo = startInfo
        };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is not null)
            {
                stdout.AppendLine(eventArgs.Data);
            }
        };
        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is not null)
            {
                stderr.AppendLine(eventArgs.Data);
            }
        };

        try
        {
            process.Start();
        }
        catch (Exception error)
        {
            return new ProcessResult(null, stdout.ToString(), stderr.ToString(), error);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        return new ProcessResult(process.ExitCode, stdout.ToString(), stderr.ToString());
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var composeFile = Path.Combine(directory.FullName, "deploy", "containers", "docker-compose.yml");

            if (File.Exists(composeFile))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing deploy/containers/docker-compose.yml.");
    }

    private static int GetPort(string environmentVariable)
    {
        var configuredValue = Environment.GetEnvironmentVariable(environmentVariable);

        if (int.TryParse(configuredValue, out var configuredPort))
        {
            return configuredPort;
        }

        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        return port;
    }

    private sealed record ComposeInvocation(string Command, string[] Args, string Label);

    private sealed record ProcessResult(int? ExitCode, string Stdout, string Stderr, Exception? Error = null);
}
