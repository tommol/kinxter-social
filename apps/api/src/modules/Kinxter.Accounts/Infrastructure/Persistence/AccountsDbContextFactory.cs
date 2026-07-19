using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kinxter.Accounts.Infrastructure.Persistence;

public sealed class AccountsDbContextFactory : IDesignTimeDbContextFactory<AccountsDbContext>
{
    public AccountsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AccountsDbContext>();

        AccountsDbContextOptions.Configure(optionsBuilder, GetConnectionString());

        return new AccountsDbContext(optionsBuilder.Options);
    }

    private static string GetConnectionString()
    {
        return Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? Environment.GetEnvironmentVariable("KINXTER_POSTGRES_CONNECTION_STRING")
            ?? "Host=localhost;Port=15432;Database=kinxter_social;Username=kinxter;Password=kinxter;GSS Encryption Mode=Disable";
    }
}
