using Kinxter.Tags.Model;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Tags.Infrastructure;

public sealed class TagsDbContext(DbContextOptions<TagsDbContext> options) : DbContext(options)
{
    public DbSet<KinkTag> Tags => Set<KinkTag>();
    public DbSet<EntityTagAssignment> Assignments => Set<EntityTagAssignment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("tags");
        builder.Entity<KinkTag>(tag =>
        {
            tag.ToTable("kink_tags");
            tag.HasKey(current => current.Id);
            tag.Property(current => current.Id).ValueGeneratedNever();
            tag.Property(current => current.Slug).IsRequired().HasMaxLength(80);
            tag.Property(current => current.Category).IsRequired().HasMaxLength(80);
            tag.Property(current => current.NamePl).IsRequired().HasMaxLength(120);
            tag.Property(current => current.NameEn).IsRequired().HasMaxLength(120);
            tag.Property(current => current.DescriptionPl).HasMaxLength(500);
            tag.Property(current => current.DescriptionEn).HasMaxLength(500);
            tag.HasIndex(current => current.Slug).IsUnique();
            tag.HasIndex(current => new { current.IsActive, current.SortOrder });
        });
        builder.Entity<EntityTagAssignment>(assignment =>
        {
            assignment.ToTable("entity_tag_assignments");
            assignment.HasKey(current => new { current.EntityType, current.EntityId, current.TagId });
            assignment.Property(current => current.EntityType).HasMaxLength(32);
            assignment.HasIndex(current => new { current.EntityType, current.EntityId });
            assignment.HasOne<KinkTag>().WithMany().HasForeignKey(current => current.TagId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
