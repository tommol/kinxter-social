using Npgsql;

namespace Kinxter.Auth;

internal static class AuthPostgresConnectionString
{
    public static string Build(string connectionString, string schema)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            SearchPath = schema
        };

        return builder.ConnectionString;
    }
}
