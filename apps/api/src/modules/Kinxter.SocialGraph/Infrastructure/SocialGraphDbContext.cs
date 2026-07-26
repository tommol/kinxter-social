using Kinxter.SocialGraph.Model;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.SocialGraph.Infrastructure;

public sealed class SocialGraphDbContext(DbContextOptions<SocialGraphDbContext> options) : DbContext(options)
{
    public DbSet<Follow> Follows => Set<Follow>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("social_graph");
        builder.Entity<Follow>(follow =>
        {
            follow.ToTable("follows"); follow.HasKey(current => new { current.FollowerProfileId, current.FollowedProfileId });
            follow.Property(current => current.Status).HasConversion<string>().HasMaxLength(32); follow.HasIndex(current => new { current.FollowedProfileId, current.Status });
        });
    }
}
