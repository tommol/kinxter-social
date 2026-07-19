using Microsoft.EntityFrameworkCore;

namespace Kinxter.Profiles.Infrastructure.Persistence;

internal static class ProfilesDbContextOptions
{
    public const string ConnectionStringName = "Postgres";

    private const string SchemaName = "profiles";
    private const string MigrationsHistoryTableName = "__ef_migrations_history";
    private static readonly string MigrationsAssembly = typeof(ProfilesDbContext).Assembly.GetName().Name!;

    public static void Configure(DbContextOptionsBuilder optionsBuilder, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        optionsBuilder.UseNpgsql(connectionString, postgresOptions =>
        {
            postgresOptions.MigrationsAssembly(MigrationsAssembly);
            postgresOptions.MigrationsHistoryTable(MigrationsHistoryTableName, SchemaName);
        });
    }
}
