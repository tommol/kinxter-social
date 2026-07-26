using Kinxter.Accounts.Application.IdentityEvents;
using Kinxter.Accounts.Application;
using Kinxter.Accounts.Contracts;
using Kinxter.IntegrationEvents.Identity;
using Kinxter.Accounts.Infrastructure.Outbox;
using Kinxter.Accounts.Infrastructure.Persistence;
using Kinxter.Shared.Abstractions.Events;
using Kinxter.Shared.Abstractions.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kinxter.Accounts;

public static class AccountsModule
{
    public static IServiceCollection AddAccountsModule(this IServiceCollection services)
    {
        AddApplicationServices(services);
        services.AddSingleton(new AccountConsentOptions());

        return services;
    }

    public static IServiceCollection AddAccountsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        AddApplicationServices(services);
        services.AddSingleton(AccountConsentOptions.FromConfiguration(configuration));

        var connectionString = configuration.GetConnectionString(AccountsDbContextOptions.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{AccountsDbContextOptions.ConnectionStringName}' is not configured.");

        services.AddDbContext<AccountsDbContext>(options =>
        {
            AccountsDbContextOptions.Configure(options, connectionString);
        });

        return services;
    }

    private static void AddApplicationServices(IServiceCollection services)
    {
        services.AddScoped<IModuleEventHandler<IdentityUserRegisteredV1>, CreateAccountOnIdentityUserRegisteredHandler>();
        services.AddScoped<IModuleEventHandler<IdentityEmailConfirmedV1>, IdentityEmailConfirmedHandler>();
        services.AddScoped<IModuleEventHandler<IdentityUserDisabledV1>, IdentityUserDisabledHandler>();
        services.AddScoped<IModuleEventHandler<IdentityUserDeletedV1>, IdentityUserDeletedHandler>();
        services.AddScoped<IOutboxWriter<AccountsOutbox>, AccountsOutboxWriter>();
        services.AddScoped<IOutboxStore, AccountsOutboxStore>();
        services.AddScoped<IAccountsService, AccountsService>();
    }
}
