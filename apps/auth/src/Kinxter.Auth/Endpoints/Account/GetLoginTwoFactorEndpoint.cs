namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static Task<IResult> GetLoginTwoFactorAsync(
        string? returnUrl,
        HttpContext context,
        AuthPageRenderer renderer)
    {
        return renderer.LoginTwoFactorAsync(context, returnUrl);
    }
}
