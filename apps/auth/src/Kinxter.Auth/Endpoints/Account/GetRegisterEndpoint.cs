namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static IResult GetRegisterAsync(string? returnUrl, AuthOptions options)
    {
        return Results.Content(AuthHtml.Register(options, returnUrl), "text/html");
    }
}
