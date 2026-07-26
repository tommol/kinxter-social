using Kinxter.Auth.Infrastructure.Persistence;
using Kinxter.Auth.Email;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static async Task<IResult> ConfirmEmailAsync(
        string userId,
        string code,
        string? returnUrl,
        HttpContext context,
        AuthDbContext dbContext,
        UserManager<AuthUser> userManager,
        AuthIntegrationEventPublisher eventPublisher,
        AuthOptions options,
        AuthPageRenderer renderer,
        CancellationToken cancellationToken)
    {
        var user = Guid.TryParse(userId, out var parsedUserId)
            ? await userManager.FindByIdAsync(parsedUserId.ToString("D"))
            : null;

        if (user is null || user.Realm != options.Realm)
        {
            return await renderer.EmailConfirmedAsync(context, options, returnUrl, succeeded: false);
        }

        if (user.EmailConfirmed)
        {
            return await renderer.EmailConfirmedAsync(context, options, returnUrl, succeeded: true);
        }

        if (!EmailConfirmationService.TryDecodeToken(code, out var decodedToken))
        {
            return await renderer.EmailConfirmedAsync(context, options, returnUrl, succeeded: false);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var result = await userManager.ConfirmEmailAsync(user, decodedToken);

        if (!result.Succeeded)
        {
            return await renderer.EmailConfirmedAsync(context, options, returnUrl, succeeded: false);
        }

        await eventPublisher.PublishEmailConfirmedAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await renderer.EmailConfirmedAsync(context, options, returnUrl, succeeded: true);
    }
}
