using Kinxter.Communities.Application;
using Kinxter.Communities.Contracts;
using Kinxter.Communities.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kinxter.Communities;

public static class CommunitiesModule
{
    public static IServiceCollection AddCommunitiesModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
        services.AddDbContext<CommunitiesDbContext>(options => options.UseNpgsql(connection, postgres => { postgres.MigrationsAssembly(typeof(CommunitiesModule).Assembly.GetName().Name); postgres.MigrationsHistoryTable("__ef_migrations_history", "communities"); }));
        services.AddScoped<ICommunitiesService, CommunitiesService>(); return services;
    }
}
