using Kinxter.Accounts.Application.RegisterAccount;
using Kinxter.Accounts.Infrastructure.Outbox;
using Kinxter.Shared.Abstractions.Application;
using Kinxter.Shared.Abstractions.Outbox;
using Microsoft.Extensions.DependencyInjection;

namespace Kinxter.Accounts;

public static class AccountsModule
{
    public static IServiceCollection AddAccountsModule(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<RegisterAccountCommand, RegisterAccountResult>, RegisterAccountHandler>();
        services.AddScoped<IOutboxWriter<AccountsOutbox>, AccountsOutboxWriter>();
        services.AddScoped<IOutboxStore, AccountsOutboxStore>();

        return services;
    }
}
