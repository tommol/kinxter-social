using Kinxter.Accounts.Contracts.Events;
using Kinxter.Profiles.Application.CreateProfileOnAccountCreated;
using Kinxter.Shared.Abstractions.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Kinxter.Profiles;

public static class ProfilesModule
{
    public static IServiceCollection AddProfilesModule(this IServiceCollection services)
    {
        services.AddScoped<IModuleEventHandler<AccountCreated>, CreateProfileOnAccountCreatedHandler>();

        return services;
    }
}
