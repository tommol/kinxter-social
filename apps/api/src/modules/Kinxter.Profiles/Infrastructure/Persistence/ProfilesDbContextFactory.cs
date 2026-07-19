using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kinxter.Profiles.Infrastructure.Persistence;

public sealed class ProfilesDbContextFactory : IDesignTimeDbContextFactory<ProfilesDbContext>
{
    public ProfilesDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProfilesDbContext>();

        ProfilesDbContextOptions.Configure(optionsBuilder, GetConnectionString());

        return new ProfilesDbContext(optionsBuilder.Options);
    }

    private static string GetConnectionString()
    {
        return Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? Environment.GetEnvironmentVariable("KINXTER_POSTGRES_CONNECTION_STRING")
            ?? "Host=localhost;Port=15432;Database=kinxter_social;Username=kinxter;Password=kinxter;GSS Encryption Mode=Disable";
    }
}
