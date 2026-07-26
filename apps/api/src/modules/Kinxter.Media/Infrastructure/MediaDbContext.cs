using Kinxter.Media.Model;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Media.Infrastructure;

public sealed class MediaDbContext(DbContextOptions<MediaDbContext> options) : DbContext(options)
{
    public DbSet<MediaAsset> Assets => Set<MediaAsset>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("media"); builder.Entity<MediaAsset>(asset =>
        { asset.ToTable("assets"); asset.HasKey(current => current.Id); asset.Property(current => current.Id).ValueGeneratedNever(); asset.Property(current => current.ObjectKey).IsRequired().HasMaxLength(500); asset.Property(current => current.ContentType).IsRequired().HasMaxLength(100); asset.Property(current => current.Status).HasConversion<string>().HasMaxLength(32); asset.HasIndex(current => new { current.AccountId, current.Status }); asset.HasIndex(current => current.ObjectKey).IsUnique(); });
    }
}
