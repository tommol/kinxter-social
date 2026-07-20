namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static IResult GetLoginTwoFactorAsync(string? returnUrl)
    {
        return Results.Content(AuthHtml.LoginTwoFactor(returnUrl), "text/html");
    }
}
