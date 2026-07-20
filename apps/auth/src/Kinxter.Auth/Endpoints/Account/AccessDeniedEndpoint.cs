namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static Task<IResult> GetAccessDeniedAsync(
        HttpContext context,
        AuthPageRenderer renderer)
    {
        return renderer.AccessDeniedAsync(context);
    }
}
