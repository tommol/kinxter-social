using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static async Task<IResult> ConfirmEmailAsync(
        string userId,
        string code,
        UserManager<AuthUser> userManager,
        AuthIntegrationEventPublisher eventPublisher,
        AuthOptions options,
        CancellationToken cancellationToken)
    {
        var user = Guid.TryParse(userId, out var parsedUserId)
            ? await userManager.FindByIdAsync(parsedUserId.ToString("D"))
            : null;

        if (user is null || user.Realm != options.Realm)
        {
            return Results.NotFound();
        }

        var result = await userManager.ConfirmEmailAsync(user, code);

        if (!result.Succeeded)
        {
            return Results.BadRequest(new { error = FormatIdentityErrors(result) });
        }

        await eventPublisher.PublishEmailConfirmedAsync(user, cancellationToken);

        return Results.Ok(new { status = "confirmed" });
    }
}
