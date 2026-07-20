using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        UserManager<AuthUser> userManager)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());

        if (user is null)
        {
            return Results.BadRequest(new { error = "Invalid password reset request." });
        }

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

        return result.Succeeded
            ? Results.Ok(new { status = "password-reset" })
            : Results.BadRequest(new { error = FormatIdentityErrors(result) });
    }
}
