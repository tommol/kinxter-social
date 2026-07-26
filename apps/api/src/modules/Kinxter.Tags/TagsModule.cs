using Kinxter.Tags.Application;
using Kinxter.Tags.Contracts;
using Kinxter.Tags.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kinxter.Tags;

public static class TagsModule
{
    public static IServiceCollection AddTagsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
        services.AddDbContext<TagsDbContext>(options => options.UseNpgsql(connectionString, postgres =>
        {
            postgres.MigrationsAssembly(typeof(TagsModule).Assembly.GetName().Name);
            postgres.MigrationsHistoryTable("__ef_migrations_history", "tags");
        }));
        services.AddScoped<ITagsService, TagsService>();
        return services;
    }
}
