using Kinxter.Locations.Model;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Locations.Infrastructure;

public sealed class LocationsDbContext(DbContextOptions<LocationsDbContext> options) : DbContext(options)
{
    public DbSet<Place> Places => Set<Place>();
    public DbSet<EntityLocation> EntityLocations => Set<EntityLocation>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("locations");
        builder.Entity<Place>(place =>
        {
            place.ToTable("places"); place.HasKey(current => current.GeoNameId); place.Property(current => current.GeoNameId).ValueGeneratedNever();
            place.Property(current => current.Name).IsRequired().HasMaxLength(200); place.Property(current => current.AdminRegion).HasMaxLength(200);
            place.Property(current => current.CountryCode).IsRequired().HasMaxLength(2); place.Ignore(current => current.DisplayName);
            place.HasIndex(current => current.Name); place.HasIndex(current => new { current.CountryCode, current.AdminRegion, current.Name });
        });
        builder.Entity<EntityLocation>(location =>
        {
            location.ToTable("entity_locations"); location.HasKey(current => new { current.EntityType, current.EntityId });
            location.Property(current => current.EntityType).HasMaxLength(32); location.HasIndex(current => current.PlaceId);
            location.HasOne<Place>().WithMany().HasForeignKey(current => current.PlaceId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
