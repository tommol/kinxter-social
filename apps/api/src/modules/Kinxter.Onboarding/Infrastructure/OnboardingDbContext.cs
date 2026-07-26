using Kinxter.Onboarding.Model;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Onboarding.Infrastructure;

public sealed class OnboardingDbContext(DbContextOptions<OnboardingDbContext> options) : DbContext(options)
{
    public DbSet<OnboardingProgress> Progress => Set<OnboardingProgress>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("onboarding");
        builder.Entity<OnboardingProgress>(progress =>
        {
            progress.ToTable("progress"); progress.HasKey(current => current.AccountId); progress.Property(current => current.AccountId).ValueGeneratedNever();
            progress.Property(current => current.InterestsStatus).HasConversion<string>().HasMaxLength(32); progress.Property(current => current.RecommendationsStatus).HasConversion<string>().HasMaxLength(32);
        });
    }
}
