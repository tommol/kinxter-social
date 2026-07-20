using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static async Task<IResult> RegisterPasskeyAsync(
        HttpContext context,
        UserManager<AuthUser> userManager,
        SignInManager<AuthUser> signInManager)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is null)
        {
            return Results.Challenge();
        }

        var credentialJson = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var attestation = await signInManager.PerformPasskeyAttestationAsync(credentialJson);

        if (!attestation.Succeeded)
        {
            return Results.BadRequest(new { error = attestation.Failure?.Message ?? "Passkey attestation failed." });
        }

        var result = await userManager.AddOrUpdatePasskeyAsync(user, attestation.Passkey);

        return result.Succeeded
            ? Results.Ok(new { status = "passkey-registered" })
            : Results.BadRequest(new { error = FormatIdentityErrors(result) });
    }
}
