using Kinxter.Accounts.Model;
using Kinxter.Shared.Abstractions.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Accounts.Infrastructure.Persistence;

public class AccountsDbContext : DbContext
{
    public AccountsDbContext(DbContextOptions<AccountsDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; } = null!;

    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("accounts");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccountsDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
