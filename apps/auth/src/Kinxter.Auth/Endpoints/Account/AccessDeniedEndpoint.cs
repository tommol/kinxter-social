namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static IResult GetAccessDeniedAsync()
    {
        return Results.Content(AuthHtml.AccessDenied(), "text/html");
    }
}
