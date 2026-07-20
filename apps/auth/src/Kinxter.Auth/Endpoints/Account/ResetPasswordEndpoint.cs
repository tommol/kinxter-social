using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        AuthDbContext dbContext,
        UserManager<AuthUser> userManager,
        AuthOptions options,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailInRealmAsync(
            dbContext,
            options,
            request.Email,
            cancellationToken);

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
