namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static IResult GetLoginAsync(string? returnUrl)
    {
        return Results.Content(AuthHtml.Login(returnUrl), "text/html");
    }
}
