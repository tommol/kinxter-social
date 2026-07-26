using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Kinxter.Shared.Abstractions.Outbox;

namespace Kinxter.Auth.Infrastructure.Persistence;

public sealed class AuthDbContext : IdentityDbContext<AuthUser, IdentityRole<Guid>, Guid>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<AuthRealm> AuthRealms { get; set; } = null!;

    public DbSet<AuthClient> AuthClients { get; set; } = null!;

    public DbSet<AuthAdministrator> AuthAdministrators { get; set; } = null!;

    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    public DbSet<AuthEmailMessage> EmailOutboxMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }
}
