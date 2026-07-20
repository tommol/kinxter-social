namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static IResult GetLoginAsync(string? returnUrl, AuthOptions options)
    {
        return Results.Content(AuthHtml.Login(options, returnUrl), "text/html");
    }
}
