using Kinxter.Auth.Email;
using Kinxter.Auth.Infrastructure.Persistence;
using Kinxter.Auth.Rendering;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static async Task<IResult> ResendConfirmationAsync(
        HttpContext context,
        AuthDbContext dbContext,
        UserManager<AuthUser> userManager,
        EmailConfirmationService confirmationService,
        AuthOptions options,
        AuthPageRenderer renderer,
        CancellationToken cancellationToken)
    {
        var form = await context.Request.ReadFormAsync(cancellationToken);
        var email = form["email"].ToString().Trim();
        var returnUrl = NormalizeReturnUrl(form["returnUrl"].ToString());
        var user = await userManager.FindByEmailInRealmAsync(dbContext, options, email, cancellationToken);

        if (user is { EmailConfirmed: false, DeletedAt: null, DisabledAt: null })
        {
            await confirmationService.QueueAsync(
                user,
                options,
                returnUrl,
                AuthUiText.ResolveLocale(context, returnUrl),
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await renderer.CheckEmailAsync(context, options, email, returnUrl);
    }
}
