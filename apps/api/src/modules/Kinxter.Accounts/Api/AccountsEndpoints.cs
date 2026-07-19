using Kinxter.Accounts.Api.RegisterAccount;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Kinxter.Accounts.Api;

public static class AccountsEndpoints
{
    public static IEndpointRouteBuilder MapAccountsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/accounts");

        group.MapRegisterAccountEndpoint();

        return app;
    }
}
