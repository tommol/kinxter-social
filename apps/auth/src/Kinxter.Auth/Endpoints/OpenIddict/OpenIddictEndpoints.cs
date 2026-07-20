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
}
