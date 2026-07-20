using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Auth.Infrastructure.Persistence;

internal static class AuthUserLookupExtensions
{
    public static Task<AuthUser?> FindByEmailInRealmAsync(
        this UserManager<AuthUser> userManager,
        AuthDbContext dbContext,
        AuthOptions options,
        string email,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(options);

        var normalizedEmail = userManager.NormalizeEmail(email.Trim());

        return dbContext.Users.SingleOrDefaultAsync(
            user => user.Realm == options.Realm && user.NormalizedEmail == normalizedEmail,
            cancellationToken);
    }
}
