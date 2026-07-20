using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static async Task<IResult> CreatePasskeyOptionsAsync(
        HttpContext context,
        UserManager<AuthUser> userManager,
        SignInManager<AuthUser> signInManager)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is null)
        {
            return Results.Challenge();
        }

        var id = await userManager.GetUserIdAsync(user);
        var username = await userManager.GetUserNameAsync(user) ?? user.Email ?? id;
        var optionsJson = await signInManager.MakePasskeyCreationOptionsAsync(new PasskeyUserEntity
        {
            Id = id,
            Name = username,
            DisplayName = username
        });

        return Results.Content(optionsJson, "application/json");
    }
}
