using Kinxter.Shared.Abstractions.Time;
using Kinxter.Tags.Model;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Tags.Infrastructure;

public static class TagsSeed
{
    public static async Task SeedAsync(TagsDbContext dbContext, IClock clock, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Tags.AnyAsync(cancellationToken)) return;
        var rows = new[]
        {
            ("bear", "subculture", "Bear", "Bear"),
            ("leather", "subculture", "Leather", "Leather"),
            ("pup-play", "kink", "Pup play", "Pup play"),
            ("bondage", "kink", "Bondage", "Bondage"),
            ("bdsm", "kink", "BDSM", "BDSM"),
            ("rubber", "subculture", "Rubber", "Rubber"),
            ("drag", "subculture", "Drag", "Drag"),
            ("queer-social", "community", "Queer social", "Queer social"),
            ("trans-community", "community", "Społeczność trans", "Trans community"),
            ("kink-positive", "community", "Kink positive", "Kink positive")
        };
        var now = clock.UtcNow;
        dbContext.Tags.AddRange(rows.Select((row, index) => new KinkTag(Guid.CreateVersion7(now.AddMilliseconds(index)), row.Item1, row.Item2, row.Item3, row.Item4, null, null, index, now)));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
