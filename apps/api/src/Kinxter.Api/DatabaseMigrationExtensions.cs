using Kinxter.Accounts.Infrastructure.Persistence;
using Kinxter.Profiles.Infrastructure.Persistence;
using Kinxter.Tags.Infrastructure;
using Kinxter.Locations.Infrastructure;
using Kinxter.Communities.Infrastructure;
using Kinxter.SocialGraph.Infrastructure;
using Kinxter.Onboarding.Infrastructure;
using Kinxter.Media.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Kinxter.Shared.Abstractions.Time;

namespace Kinxter.Api;

internal static class DatabaseMigrationExtensions
{
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        await services.GetRequiredService<AccountsDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<ProfilesDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<TagsDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<LocationsDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<CommunitiesDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<SocialGraphDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<OnboardingDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<MediaDbContext>().Database.MigrateAsync();
        await TagsSeed.SeedAsync(
            services.GetRequiredService<TagsDbContext>(),
            services.GetRequiredService<IClock>());
        await LocationsSeed.SeedAsync(
            services.GetRequiredService<LocationsDbContext>(),
            app.Configuration["GeoNames:ImportFile"]);
    }
}
