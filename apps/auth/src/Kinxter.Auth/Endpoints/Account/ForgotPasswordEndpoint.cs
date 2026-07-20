using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static async Task<IResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        UserManager<AuthUser> userManager,
        IWebHostEnvironment environment)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());

        if (user is null || user.Email is null)
        {
            return Results.Accepted();
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        return environment.IsDevelopment()
            ? Results.Ok(new { status = "reset-token-created", token })
            : Results.Accepted();
    }
}
