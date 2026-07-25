using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Auth;

internal static partial class OpenIddictEndpoints
{
    public static IEndpointRouteBuilder MapOpenIddictEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapMethods("/connect/authorize", [HttpMethods.Get, HttpMethods.Post], AuthorizeAsync);
        app.MapPost("/connect/token", ExchangeAsync);
        app.MapMethods("/connect/logout", [HttpMethods.Get, HttpMethods.Post], LogoutAsync);
        app.MapMethods("/connect/userinfo", [HttpMethods.Get, HttpMethods.Post], UserInfoAsync);

        return app;
    }

    private static Task<bool> IsClientEnabledForRealmAsync(
        AuthDbContext dbContext,
        string? clientId,
        AuthOptions authOptions,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return Task.FromResult(false);
        }

        return dbContext.AuthClients
            .AsNoTracking()
            .AnyAsync(
                client => client.ClientId == clientId &&
                    client.Enabled &&
                    client.Realm.Name == authOptions.Realm,
                cancellationToken);
    }
}
