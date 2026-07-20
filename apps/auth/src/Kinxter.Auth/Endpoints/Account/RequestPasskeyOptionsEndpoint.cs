using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static async Task<IResult> RequestPasskeyOptionsAsync(
        HttpContext context,
        UserManager<AuthUser> userManager,
        SignInManager<AuthUser> signInManager)
    {
        var username = context.Request.Query["username"].ToString();
        var user = string.IsNullOrWhiteSpace(username)
            ? null
            : await userManager.FindByNameAsync(username);
        var optionsJson = await signInManager.MakePasskeyRequestOptionsAsync(user);

        return Results.Content(optionsJson, "application/json");
    }
}
