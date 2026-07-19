using Kinxter.Profiles.Model;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Profiles.Infrastructure.Persistence;

public sealed class ProfilesDbContext : DbContext
{
    public ProfilesDbContext(DbContextOptions<ProfilesDbContext> options) : base(options)
    {
    }

    public DbSet<Profile> Profiles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("profiles");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProfilesDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
