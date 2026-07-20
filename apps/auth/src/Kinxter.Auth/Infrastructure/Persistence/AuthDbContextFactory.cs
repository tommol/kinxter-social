using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kinxter.Auth.Infrastructure.Persistence;

public sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
        var schema = Environment.GetEnvironmentVariable("Auth__DbSchema") ?? "auth_public";

        optionsBuilder.UseNpgsql(AuthPostgresConnectionString.Build(GetConnectionString(), schema));
        optionsBuilder.UseOpenIddict();

        return new AuthDbContext(optionsBuilder.Options);
    }

    private static string GetConnectionString()
    {
        return Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? Environment.GetEnvironmentVariable("KINXTER_POSTGRES_CONNECTION_STRING")
            ?? "Host=localhost;Port=15432;Database=kinxter_social;Username=kinxter;Password=kinxter;GSS Encryption Mode=Disable";
    }
}
