namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static Task<IResult> GetLoginAsync(
        string? returnUrl,
        AuthOptions options,
        HttpContext context,
        AuthPageRenderer renderer)
    {
        return renderer.LoginAsync(context, options, returnUrl);
    }
}
