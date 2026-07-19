using Microsoft.EntityFrameworkCore;

namespace Kinxter.Accounts.Infrastructure.Persistence;

internal static class AccountsDbContextOptions
{
    public const string ConnectionStringName = "Postgres";

    private const string SchemaName = "accounts";
    private const string MigrationsHistoryTableName = "__ef_migrations_history";
    private static readonly string MigrationsAssembly = typeof(AccountsDbContext).Assembly.GetName().Name!;

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
