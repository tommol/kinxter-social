using Kinxter.Media.Application;
using Kinxter.Media.Contracts;
using Kinxter.Media.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kinxter.Media;

public static class MediaModule
{
    public static IServiceCollection AddMediaModule(this IServiceCollection services, IConfiguration configuration)
    { var connection = configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured."); services.AddSingleton(MediaStorageOptions.FromConfiguration(configuration)); services.AddHttpClient("media-storage"); services.AddScoped<IMediaService, MediaService>(); services.AddDbContext<MediaDbContext>(options => options.UseNpgsql(connection, postgres => { postgres.MigrationsAssembly(typeof(MediaModule).Assembly.GetName().Name); postgres.MigrationsHistoryTable("__ef_migrations_history", "media"); })); return services; }
}
