using Kinxter.Locations.Application;
using Kinxter.Locations.Contracts;
using Kinxter.Locations.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kinxter.Locations;

public static class LocationsModule
{
    public static IServiceCollection AddLocationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
        services.AddDbContext<LocationsDbContext>(options => options.UseNpgsql(connection, postgres =>
        {
            postgres.MigrationsAssembly(typeof(LocationsModule).Assembly.GetName().Name);
            postgres.MigrationsHistoryTable("__ef_migrations_history", "locations");
        }));
        services.AddScoped<ILocationsService, LocationsService>();
        return services;
    }
}
