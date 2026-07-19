using System.Text;

namespace Kinxter.Shared.Infrastructure.Events.Nats;

internal static class NatsModuleEventSubject
{
    public static string FromEventType(string subjectPrefix, Type eventType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectPrefix);
        ArgumentNullException.ThrowIfNull(eventType);

        return $"{subjectPrefix}.{GetModuleName(eventType)}.{ToKebabCase(eventType.Name)}";
    }

    public static string All(string subjectPrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectPrefix);

        return $"{subjectPrefix}.>";
    }

    private static string GetModuleName(Type eventType)
    {
        var segments = eventType.Namespace?.Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (segments is { Length: > 1 } && string.Equals(segments[0], "Kinxter", StringComparison.Ordinal))
        {
            return ToKebabCase(segments[1]);
        }

        return "module";
    }

    private static string ToKebabCase(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var builder = new StringBuilder(value.Length + 8);

        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];

            if (char.IsUpper(current) && index > 0 && value[index - 1] != '-')
            {
                builder.Append('-');
            }

            builder.Append(char.ToLowerInvariant(current));
        }

        return builder.ToString();
    }
}
