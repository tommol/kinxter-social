using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static async Task<IResult> RegisterAsync(
        HttpContext context,
        UserManager<AuthUser> userManager,
        SignInManager<AuthUser> signInManager,
        AuthIntegrationEventPublisher eventPublisher,
        AuthOptions options,
        CancellationToken cancellationToken)
    {
        if (!options.SignupEnabled)
        {
            return Results.NotFound();
        }

        var form = await context.Request.ReadFormAsync(cancellationToken);
        var email = form["email"].ToString().Trim();
        var password = form["password"].ToString();
        var returnUrl = NormalizeReturnUrl(form["returnUrl"].ToString());

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return Results.Content(AuthHtml.Register(options, returnUrl, "Email and password are required."), "text/html");
        }

        var user = new AuthUser
        {
            Id = Guid.CreateVersion7(),
            Realm = options.Realm,
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            return Results.Content(AuthHtml.Register(options, returnUrl, FormatIdentityErrors(result)), "text/html");
        }

        await eventPublisher.PublishUserRegisteredAsync(user, cancellationToken);
        await signInManager.SignInAsync(user, isPersistent: false);

        return Results.Redirect(returnUrl);
    }
}
