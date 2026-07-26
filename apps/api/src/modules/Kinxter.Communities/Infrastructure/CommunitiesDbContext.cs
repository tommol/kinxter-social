using Kinxter.Communities.Model;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Communities.Infrastructure;

public sealed class CommunitiesDbContext(DbContextOptions<CommunitiesDbContext> options) : DbContext(options)
{
    public DbSet<Community> Communities => Set<Community>();
    public DbSet<CommunityMembership> Memberships => Set<CommunityMembership>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("communities");
        builder.Entity<Community>(community =>
        {
            community.ToTable("communities"); community.HasKey(current => current.Id); community.Property(current => current.Id).ValueGeneratedNever();
            community.Property(current => current.Slug).IsRequired().HasMaxLength(80); community.Property(current => current.Name).IsRequired().HasMaxLength(120);
            community.Property(current => current.Description).IsRequired().HasMaxLength(2000); community.Property(current => current.Status).HasConversion<string>().HasMaxLength(32);
            community.Property(current => current.RejectionReason).HasMaxLength(1000); community.HasIndex(current => current.Slug).IsUnique(); community.HasIndex(current => current.Status);
        });
        builder.Entity<CommunityMembership>(membership =>
        {
            membership.ToTable("memberships"); membership.HasKey(current => new { current.CommunityId, current.ProfileId });
            membership.HasIndex(current => current.ProfileId); membership.HasOne<Community>().WithMany().HasForeignKey(current => current.CommunityId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
