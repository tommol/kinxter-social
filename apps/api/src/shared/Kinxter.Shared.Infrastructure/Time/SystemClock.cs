using Kinxter.Shared.Abstractions.Time;

namespace Kinxter.Shared.Infrastructure.Time;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
