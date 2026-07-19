using Kinxter.Accounts.Infrastructure.Persistence;
using Kinxter.Profiles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Api;

internal static class DatabaseMigrationExtensions
{
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        await services.GetRequiredService<AccountsDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<ProfilesDbContext>().Database.MigrateAsync();
    }
}
