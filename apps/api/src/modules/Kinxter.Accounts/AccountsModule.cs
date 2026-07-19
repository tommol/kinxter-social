using Kinxter.Accounts.Application.RegisterAccount;
using Kinxter.Accounts.Infrastructure.Outbox;
using Kinxter.Accounts.Infrastructure.Persistence;
using Kinxter.Shared.Abstractions.Application;
using Kinxter.Shared.Abstractions.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kinxter.Accounts;

public static class AccountsModule
{
    public static IServiceCollection AddAccountsModule(this IServiceCollection services)
    {
        AddApplicationServices(services);

        return services;
    }

    public static IServiceCollection AddAccountsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        AddApplicationServices(services);

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
        services.AddScoped<ICommandHandler<RegisterAccountCommand, RegisterAccountResult>, RegisterAccountHandler>();
        services.AddScoped<IOutboxWriter<AccountsOutbox>, AccountsOutboxWriter>();
        services.AddScoped<IOutboxStore, AccountsOutboxStore>();
    }
}
