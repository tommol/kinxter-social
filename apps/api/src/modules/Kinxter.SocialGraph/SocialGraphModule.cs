using Kinxter.SocialGraph.Application;
using Kinxter.SocialGraph.Contracts;
using Kinxter.SocialGraph.Infrastructure;
using Kinxter.Profiles.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kinxter.SocialGraph;

public static class SocialGraphModule
{
    public static IServiceCollection AddSocialGraphModule(this IServiceCollection services, IConfiguration configuration)
    { var connection = configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured."); services.AddDbContext<SocialGraphDbContext>(options => options.UseNpgsql(connection, postgres => { postgres.MigrationsAssembly(typeof(SocialGraphModule).Assembly.GetName().Name); postgres.MigrationsHistoryTable("__ef_migrations_history", "social_graph"); })); services.AddScoped<ISocialGraphService, SocialGraphService>(); services.AddScoped<IProfileVisibilityChangedListener, ProfileVisibilityChangedListener>(); services.AddScoped<IProfileAccessEvaluator, ProfileAccessEvaluator>(); return services; }
}
