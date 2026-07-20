namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static Task<IResult> GetRegisterAsync(
        string? returnUrl,
        AuthOptions options,
        HttpContext context,
        AuthPageRenderer renderer)
    {
        return renderer.RegisterAsync(context, options, returnUrl);
    }
}
