using Microsoft.Extensions.Configuration;

namespace Kinxter.Shared.Infrastructure.Events.Nats;

internal sealed class NatsModuleEventOptions
{
    public const string SectionName = "ModuleEvents:Nats";

    public string Url { get; init; } = "nats://localhost:4222";

    public string StreamName { get; init; } = "KINXTER_MODULE_EVENTS";

    public string SubjectPrefix { get; init; } = "kinxter.events";

    public string ConsumerName { get; init; } = "kinxter-api";

    public bool ConsumerEnabled { get; init; } = true;

    public int MaxAckPending { get; init; } = 50;

    public int MaxDeliver { get; init; } = 10;

    public TimeSpan AckWait { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan ReconnectDelay { get; init; } = TimeSpan.FromSeconds(5);

    public TimeSpan DuplicateWindow { get; init; } = TimeSpan.FromDays(1);

    public static NatsModuleEventOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new NatsModuleEventOptions
        {
            Url = GetString(configuration, "Url", "nats://localhost:4222"),
            StreamName = GetString(configuration, "StreamName", "KINXTER_MODULE_EVENTS"),
            SubjectPrefix = GetSubjectPrefix(configuration, "SubjectPrefix", "kinxter.events"),
            ConsumerName = GetString(configuration, "ConsumerName", "kinxter-api"),
            ConsumerEnabled = GetBool(configuration, "ConsumerEnabled", true),
            MaxAckPending = GetPositiveInt(configuration, "MaxAckPending", 50),
            MaxDeliver = GetPositiveInt(configuration, "MaxDeliver", 10),
            AckWait = TimeSpan.FromSeconds(GetPositiveInt(configuration, "AckWaitSeconds", 30)),
            ReconnectDelay = TimeSpan.FromSeconds(GetPositiveInt(configuration, "ReconnectDelaySeconds", 5)),
            DuplicateWindow = TimeSpan.FromSeconds(GetPositiveInt(configuration, "DuplicateWindowSeconds", 86_400))
        };
    }

    private static string GetString(IConfiguration configuration, string key, string fallback)
    {
        var value = configuration[key];

        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
    }

    private static string GetSubjectPrefix(IConfiguration configuration, string key, string fallback)
    {
        var value = GetString(configuration, key, fallback).Trim('.');

        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value;
    }

    private static int GetPositiveInt(IConfiguration configuration, string key, int fallback)
    {
        return int.TryParse(configuration[key], out var value) && value > 0
            ? value
            : fallback;
    }

    private static bool GetBool(IConfiguration configuration, string key, bool fallback)
    {
        return bool.TryParse(configuration[key], out var value)
            ? value
            : fallback;
    }
}
