using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static async Task<IResult> GenerateRecoveryCodesAsync(
        HttpContext context,
        UserManager<AuthUser> userManager)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is null)
        {
            return Results.Challenge();
        }

        var codes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

        return Results.Ok(new { recoveryCodes = codes });
    }
}
