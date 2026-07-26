using System.Security.Claims;
using Kinxter.Auth.Infrastructure.Persistence;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Auth.Administration;

internal static class AuthAdminEndpoints
{
    public static IEndpointRouteBuilder MapAuthAdminEndpoints(
        this IEndpointRouteBuilder app,
        AuthAdminOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Enabled)
        {
            return app;
        }

        var group = app.MapGroup(options.PathBase);

        group.MapGet("/login", GetLoginAsync);
        group.MapPost("/login", LoginAsync)
            .RequireRateLimiting(AuthAdminDefaults.LoginRateLimitPolicy);

        group.MapGet("/", DashboardAsync)
            .RequireAuthorization(AuthAdminDefaults.AuthorizationPolicy);
        group.MapPost("/logout", LogoutAsync)
            .RequireAuthorization(AuthAdminDefaults.AuthorizationPolicy);
        group.MapGet("/realms/{realmId:guid}", GetRealmAsync)
            .RequireAuthorization(AuthAdminDefaults.AuthorizationPolicy);
        group.MapPost("/realms/{realmId:guid}", UpdateRealmAsync)
            .RequireAuthorization(AuthAdminDefaults.AuthorizationPolicy);
        group.MapGet("/realms/{realmId:guid}/clients/new", GetNewClientAsync)
            .RequireAuthorization(AuthAdminDefaults.AuthorizationPolicy);
        group.MapPost("/realms/{realmId:guid}/clients", CreateClientAsync)
            .RequireAuthorization(AuthAdminDefaults.AuthorizationPolicy);
        group.MapGet("/realms/{realmId:guid}/clients/{clientId:guid}", GetClientAsync)
            .RequireAuthorization(AuthAdminDefaults.AuthorizationPolicy);
        group.MapPost("/realms/{realmId:guid}/clients/{clientId:guid}", UpdateClientAsync)
            .RequireAuthorization(AuthAdminDefaults.AuthorizationPolicy);
        group.MapPost("/realms/{realmId:guid}/clients/{clientId:guid}/rotate-secret", RotateClientSecretAsync)
            .RequireAuthorization(AuthAdminDefaults.AuthorizationPolicy);

        return app;
    }

    private static async Task<IResult> GetLoginAsync(
        HttpContext context,
        AuthAdminOptions options,
        AuthAdminPageRenderer renderer,
        string? returnUrl)
    {
        var authentication = await context.AuthenticateAsync(AuthAdminDefaults.AuthenticationScheme);

        if (authentication.Succeeded)
        {
            return Results.Redirect(NormalizeReturnUrl(returnUrl, options));
        }

        return await renderer.LoginAsync(
            context,
            options,
            NormalizeReturnUrl(returnUrl, options));
    }

    private static async Task<IResult> LoginAsync(
        HttpContext context,
        AuthDbContext dbContext,
        ILookupNormalizer normalizer,
        IPasswordHasher<AuthAdministrator> passwordHasher,
        IClock clock,
        AuthAdminOptions options,
        AuthAdminPageRenderer renderer,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        if (!await IsAntiforgeryValidAsync(context, antiforgery))
        {
            return Results.BadRequest();
        }

        var form = await context.Request.ReadFormAsync(cancellationToken);
        var username = form["username"].ToString().Trim();
        var password = form["password"].ToString();
        var returnUrl = NormalizeReturnUrl(form["returnUrl"].ToString(), options);
        var normalizedUsername = normalizer.NormalizeName(username);
        var administrator = string.IsNullOrWhiteSpace(normalizedUsername)
            ? null
            : await dbContext.AuthAdministrators.SingleOrDefaultAsync(
                current => current.NormalizedUsername == normalizedUsername,
                cancellationToken);

        if (administrator is null || !administrator.Enabled)
        {
            return await renderer.LoginAsync(
                context,
                options,
                returnUrl,
                "Invalid credentials.");
        }

        var verification = passwordHasher.VerifyHashedPassword(
            administrator,
            administrator.PasswordHash,
            password);

        if (verification == PasswordVerificationResult.Failed)
        {
            return await renderer.LoginAsync(
                context,
                options,
                returnUrl,
                "Invalid credentials.");
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            administrator.PasswordHash = passwordHasher.HashPassword(administrator, password);
            administrator.UpdatedAt = clock.UtcNow;
        }

        administrator.LastSignedInAt = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, administrator.Id.ToString("D")),
                new Claim(ClaimTypes.Name, administrator.Username),
                new Claim(AuthAdminDefaults.AccessClaim, bool.TrueString)
            ],
            AuthAdminDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await context.SignInAsync(
            AuthAdminDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true
            });

        return Results.Redirect(returnUrl);
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext context,
        AuthAdminOptions options,
        IAntiforgery antiforgery)
    {
        if (!await IsAntiforgeryValidAsync(context, antiforgery))
        {
            return Results.BadRequest();
        }

        await context.SignOutAsync(AuthAdminDefaults.AuthenticationScheme);

        return Results.Redirect(options.LoginPath);
    }

    private static async Task<IResult> DashboardAsync(
        HttpContext context,
        AuthAdministrationService administration,
        AuthAdminOptions options,
        AuthAdminPageRenderer renderer,
        CancellationToken cancellationToken)
    {
        var realms = await administration.GetRealmsAsync(cancellationToken);

        return await renderer.DashboardAsync(
            context,
            options,
            GetAdministratorName(context.User),
            realms);
    }

    private static async Task<IResult> GetRealmAsync(
        Guid realmId,
        HttpContext context,
        AuthAdministrationService administration,
        AuthAdminOptions options,
        AuthAdminPageRenderer renderer,
        CancellationToken cancellationToken,
        bool saved = false)
    {
        var realm = await administration.GetRealmAsync(realmId, cancellationToken);

        if (realm is null)
        {
            return Results.NotFound();
        }

        return await renderer.RealmAsync(
            context,
            options,
            GetAdministratorName(context.User),
            realm,
            saved: saved);
    }

    private static async Task<IResult> UpdateRealmAsync(
        Guid realmId,
        HttpContext context,
        AuthAdministrationService administration,
        AuthAdminOptions options,
        AuthAdminPageRenderer renderer,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        if (!await IsAntiforgeryValidAsync(context, antiforgery))
        {
            return Results.BadRequest();
        }

        var form = await context.Request.ReadFormAsync(cancellationToken);

        if (!Enum.TryParse<AuthMfaPolicy>(
                form["mfaPolicy"].ToString(),
                ignoreCase: true,
                out var mfaPolicy) ||
            !Enum.IsDefined(mfaPolicy))
        {
            return Results.BadRequest("Invalid MFA policy.");
        }

        var command = new AuthAdminUpdateRealmCommand(
            form["issuer"].ToString(),
            form["pathBase"].ToString(),
            mfaPolicy,
            form.ContainsKey("signupEnabled"));
        var result = await administration.UpdateRealmAsync(
            realmId,
            command,
            cancellationToken);

        if (result.RealmNotFound)
        {
            return Results.NotFound();
        }

        if (result.Success)
        {
            return Results.Redirect($"{options.PathBase}/realms/{realmId:D}?saved=true");
        }

        var realm = await administration.GetRealmAsync(realmId, cancellationToken);

        return realm is null
            ? Results.NotFound()
            : await renderer.RealmAsync(
                context,
                options,
                GetAdministratorName(context.User),
                realm,
                command,
                result.Error);
    }

    private static async Task<IResult> GetNewClientAsync(
        Guid realmId,
        HttpContext context,
        AuthAdministrationService administration,
        AuthAdminOptions options,
        AuthAdminPageRenderer renderer,
        CancellationToken cancellationToken)
    {
        var realm = await administration.GetRealmAsync(realmId, cancellationToken);

        return realm is null
            ? Results.NotFound()
            : await renderer.ClientAsync(
                context,
                options,
                GetAdministratorName(context.User),
                realm);
    }

    private static async Task<IResult> CreateClientAsync(
        Guid realmId,
        HttpContext context,
        AuthAdministrationService administration,
        AuthClientAdministrationService clientAdministration,
        AuthAdminOptions options,
        AuthAdminPageRenderer renderer,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        if (!await IsAntiforgeryValidAsync(context, antiforgery))
        {
            return Results.BadRequest();
        }

        var form = await context.Request.ReadFormAsync(cancellationToken);
        var clientType = ParseClientType(form["clientType"].ToString());
        var command = new AuthAdminCreateClientCommand(
            form["clientId"].ToString(),
            form["displayName"].ToString(),
            clientType,
            GetFormValues(form, "grantTypes"),
            SplitLines(form["redirectUris"].ToString()),
            SplitLines(form["postLogoutRedirectUris"].ToString()),
            SplitScopes(form["scopes"].ToString()));
        var result = await clientAdministration.CreateClientAsync(
            realmId,
            command,
            cancellationToken);

        if (result.RealmNotFound)
        {
            return Results.NotFound();
        }

        var realm = await administration.GetRealmAsync(realmId, cancellationToken);

        if (realm is null)
        {
            return Results.NotFound();
        }

        return result.Success
            ? await renderer.ClientAsync(
                context,
                options,
                GetAdministratorName(context.User),
                realm,
                result.Client,
                clientSecret: result.ClientSecret)
            : await renderer.ClientAsync(
                context,
                options,
                GetAdministratorName(context.User),
                realm,
                attemptedCreate: command,
                error: result.Error);
    }

    private static async Task<IResult> GetClientAsync(
        Guid realmId,
        Guid clientId,
        HttpContext context,
        AuthAdministrationService administration,
        AuthClientAdministrationService clientAdministration,
        AuthAdminOptions options,
        AuthAdminPageRenderer renderer,
        CancellationToken cancellationToken,
        bool saved = false)
    {
        var realm = await administration.GetRealmAsync(realmId, cancellationToken);
        var client = await clientAdministration.GetClientAsync(realmId, clientId, cancellationToken);

        return realm is null || client is null
            ? Results.NotFound()
            : await renderer.ClientAsync(
                context,
                options,
                GetAdministratorName(context.User),
                realm,
                client,
                saved: saved);
    }

    private static async Task<IResult> UpdateClientAsync(
        Guid realmId,
        Guid clientId,
        HttpContext context,
        AuthAdministrationService administration,
        AuthClientAdministrationService clientAdministration,
        AuthAdminOptions options,
        AuthAdminPageRenderer renderer,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        if (!await IsAntiforgeryValidAsync(context, antiforgery))
        {
            return Results.BadRequest();
        }

        var form = await context.Request.ReadFormAsync(cancellationToken);
        var clientType = ParseClientType(form["clientType"].ToString());
        var command = new AuthAdminUpdateClientCommand(
            form["displayName"].ToString(),
            form.ContainsKey("enabled"),
            clientType,
            GetFormValues(form, "grantTypes"),
            SplitLines(form["redirectUris"].ToString()),
            SplitLines(form["postLogoutRedirectUris"].ToString()),
            SplitScopes(form["scopes"].ToString()));
        var result = await clientAdministration.UpdateClientAsync(
            realmId,
            clientId,
            command,
            cancellationToken);

        if (result.ClientNotFound)
        {
            return Results.NotFound();
        }

        if (result.Success)
        {
            if (!string.IsNullOrWhiteSpace(result.ClientSecret))
            {
                var updatedRealm = await administration.GetRealmAsync(realmId, cancellationToken);

                return updatedRealm is null || result.Client is null
                    ? Results.NotFound()
                    : await renderer.ClientAsync(
                        context,
                        options,
                        GetAdministratorName(context.User),
                        updatedRealm,
                        result.Client,
                        clientSecret: result.ClientSecret,
                        saved: true);
            }

            return Results.Redirect(
                $"{options.PathBase}/realms/{realmId:D}/clients/{clientId:D}?saved=true");
        }

        var realm = await administration.GetRealmAsync(realmId, cancellationToken);
        var client = await clientAdministration.GetClientAsync(realmId, clientId, cancellationToken);

        return realm is null || client is null
            ? Results.NotFound()
            : await renderer.ClientAsync(
                context,
                options,
                GetAdministratorName(context.User),
                realm,
                client,
                attemptedUpdate: command,
                error: result.Error);
    }

    private static async Task<IResult> RotateClientSecretAsync(
        Guid realmId,
        Guid clientId,
        HttpContext context,
        AuthAdministrationService administration,
        AuthClientAdministrationService clientAdministration,
        AuthAdminOptions options,
        AuthAdminPageRenderer renderer,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        if (!await IsAntiforgeryValidAsync(context, antiforgery))
        {
            return Results.BadRequest();
        }

        var result = await clientAdministration.RotateSecretAsync(
            realmId,
            clientId,
            cancellationToken);

        if (result.ClientNotFound)
        {
            return Results.NotFound();
        }

        var realm = await administration.GetRealmAsync(realmId, cancellationToken);

        if (realm is null)
        {
            return Results.NotFound();
        }

        var client = result.Client ?? await clientAdministration.GetClientAsync(
            realmId,
            clientId,
            cancellationToken);

        if (client is null)
        {
            return Results.NotFound();
        }

        return await renderer.ClientAsync(
            context,
            options,
            GetAdministratorName(context.User),
            realm,
            client,
            error: result.Error,
            clientSecret: result.ClientSecret);
    }

    private static string[] SplitLines(string value)
    {
        return value.Split(
            ['\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string[] SplitScopes(string value)
    {
        return value.Split(
            ['\r', '\n', ',', ' ', '\t'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static AuthClientType ParseClientType(string value)
    {
        return Enum.TryParse<AuthClientType>(value, ignoreCase: true, out var clientType)
            ? clientType
            : (AuthClientType)0;
    }

    private static string[] GetFormValues(IFormCollection form, string key)
    {
        return form[key]
            .Where(value => value is not null)
            .Select(value => value!)
            .ToArray();
    }

    private static async Task<bool> IsAntiforgeryValidAsync(
        HttpContext context,
        IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }

    private static string GetAdministratorName(ClaimsPrincipal principal)
    {
        return principal.Identity?.Name ?? "administrator";
    }

    private static string NormalizeReturnUrl(string? returnUrl, AuthAdminOptions options)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) ||
            !returnUrl.StartsWith("/", StringComparison.Ordinal) ||
            returnUrl.StartsWith("//", StringComparison.Ordinal) ||
            (!string.Equals(returnUrl, options.PathBase, StringComparison.Ordinal) &&
             !returnUrl.StartsWith($"{options.PathBase}/", StringComparison.Ordinal)))
        {
            return options.PathBase;
        }

        return returnUrl;
    }
}
