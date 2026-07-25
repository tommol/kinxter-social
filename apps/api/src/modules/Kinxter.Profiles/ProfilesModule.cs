using Kinxter.Profiles.Application.CompleteProfileOnboarding;
using Kinxter.Profiles.Application.CreateCurrentProfile;
using Kinxter.Profiles.Infrastructure.Persistence;
using Kinxter.Shared.Abstractions.Application;
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
        services.AddScoped<ICommandHandler<CreateCurrentProfileCommand, CreateCurrentProfileResult>, CreateCurrentProfileHandler>();
        services.AddScoped<ICommandHandler<CompleteProfileOnboardingCommand, CompleteProfileOnboardingResult>, CompleteProfileOnboardingHandler>();
    }
}
