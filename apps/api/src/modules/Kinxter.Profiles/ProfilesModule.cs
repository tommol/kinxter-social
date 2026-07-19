using Kinxter.Accounts.Contracts.Events;
using Kinxter.Profiles.Application.CreateProfileOnAccountCreated;
using Kinxter.Profiles.Infrastructure.Persistence;
using Kinxter.Shared.Abstractions.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kinxter.Profiles;

public static class ProfilesModule
{
    public static IServiceCollection AddProfilesModule(this IServiceCollection services)
    {
        AddApplicationServices(services);

        return services;
    }

    public static IServiceCollection AddProfilesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        AddApplicationServices(services);

        var connectionString = configuration.GetConnectionString(ProfilesDbContextOptions.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ProfilesDbContextOptions.ConnectionStringName}' is not configured.");

        services.AddDbContext<ProfilesDbContext>(options =>
        {
            ProfilesDbContextOptions.Configure(options, connectionString);
        });

        return services;
    }

    private static void AddApplicationServices(IServiceCollection services)
    {
        services.AddScoped<IModuleEventHandler<AccountCreated>, CreateProfileOnAccountCreatedHandler>();
    }
}
