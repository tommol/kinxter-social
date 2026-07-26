using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static Task<IResult> GetActivateInvitationAsync(
        string? email,
        string? token,
        HttpContext context,
        AuthOptions options,
        AuthPageRenderer renderer)
    {
        return renderer.ActivateInvitationAsync(
            context,
            options,
            email?.Trim() ?? "",
            token ?? "");
    }

    private static async Task<IResult> ActivateInvitationAsync(
        HttpContext context,
        AuthDbContext dbContext,
        UserManager<AuthUser> userManager,
        AuthOptions options,
        AuthPageRenderer renderer,
        CancellationToken cancellationToken)
    {
        var form = await context.Request.ReadFormAsync(cancellationToken);
        var email = form["email"].ToString().Trim();
        var token = form["token"].ToString();
        var password = form["password"].ToString();
        var confirmPassword = form["confirmPassword"].ToString();

        if (!string.Equals(options.Realm, AuthRealmNames.Backoffice, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(token))
        {
            return Results.NotFound();
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            return await renderer.ActivateInvitationAsync(
                context,
                options,
                email,
                token,
                "Passwords do not match.");
        }

        var user = await userManager.FindByEmailInRealmAsync(
            dbContext,
            options,
            email,
            cancellationToken);

        if (user is null ||
            user.DisabledAt is not null ||
            user.DeletedAt is not null ||
            await userManager.HasPasswordAsync(user))
        {
            return await renderer.ActivateInvitationAsync(
                context,
                options,
                email,
                token,
                "The invitation is invalid or has already been used.");
        }

        var resetResult = await userManager.ResetPasswordAsync(user, token, password);

        if (!resetResult.Succeeded)
        {
            return await renderer.ActivateInvitationAsync(
                context,
                options,
                email,
                token,
                FormatIdentityErrors(resetResult));
        }

        user.EmailConfirmed = true;
        var updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            return await renderer.ActivateInvitationAsync(
                context,
                options,
                email,
                token,
                FormatIdentityErrors(updateResult));
        }

        return await renderer.ActivateInvitationAsync(
            context,
            options,
            email,
            "",
            completed: true);
    }
}
