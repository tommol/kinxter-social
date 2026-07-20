using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static async Task<IResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        AuthDbContext dbContext,
        UserManager<AuthUser> userManager,
        AuthOptions options,
        IWebHostEnvironment environment)
    {
        var user = await userManager.FindByEmailInRealmAsync(dbContext, options, request.Email);

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
