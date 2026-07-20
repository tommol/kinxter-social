using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static async Task<IResult> LogoutAsync(SignInManager<AuthUser> signInManager)
    {
        await signInManager.SignOutAsync();

        return Results.Redirect("/");
    }
}
